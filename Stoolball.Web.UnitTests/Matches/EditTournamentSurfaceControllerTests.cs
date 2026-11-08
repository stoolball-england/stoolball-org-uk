using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class EditTournamentSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<ITournamentDataSource> _tournamentDataSource = new();
        private readonly Mock<ITournamentRepository> _tournamentRepository = new();
        private readonly Mock<IMatchListingCacheInvalidator> _cacheClearer = new();
        private readonly Mock<IAuthorizationPolicy<Tournament>> _authorizationPolicy = new();
        private readonly Mock<IDateTimeFormatter> _dateTimeFormatter = new();
        private readonly Mock<IMatchValidator> _matchValidator = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly Dictionary<string, StringValues> _form = new()
        {
            { "Tournament.TournamentNotes", "<p>Notes</p>" },
            { "TournamentDate", "2026-06-01" }
        };
        private readonly Tournament _beforeUpdate = new()
        {
            TournamentId = Guid.NewGuid(),
            TournamentName = "Old name",
            TournamentRoute = "/tournaments/old-name",
            Seasons = [new Season { SeasonId = Guid.NewGuid(), Competition = new Competition { CompetitionName = "Example league" } }]
        };
        private readonly Tournament _updatedTournament = new() { TournamentRoute = "/tournaments/updated-tournament" };

        public EditTournamentSurfaceControllerTests()
        {
            Request.SetupGet(x => x.Form).Returns(() => new FormCollection(_form));
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).ReturnsAsync(_beforeUpdate);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTournament, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _tournamentRepository.Setup(x => x.UpdateTournament(It.IsAny<Tournament>(), _currentMember.Key, _currentMember.Name!)).ReturnsAsync(_updatedTournament);
            _dateTimeFormatter.Setup(x => x.FormatDate(It.IsAny<DateTimeOffset>(), false, false, false)).Returns("1 June 2026");
        }

        private EditTournamentSurfaceController CreateController()
        {
            return new EditTournamentSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _tournamentDataSource.Object,
                _tournamentRepository.Object,
                _cacheClearer.Object,
                _authorizationPolicy.Object,
                _dateTimeFormatter.Object,
                _matchValidator.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private static Tournament ValidTournament() => new() { TournamentName = "New name" };

        private void VerifyNothingUpdated()
        {
            _tournamentRepository.Verify(x => x.UpdateTournament(It.IsAny<Tournament>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _cacheClearer.Verify(x => x.InvalidateCacheForTournament(It.IsAny<Tournament>(), It.IsAny<Tournament>()), Times.Never);
        }

        [Fact]
        public async Task Null_tournament_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.UpdateTournament(null!));
        }

        [Fact]
        public async Task Tournament_id_route_and_seasons_are_taken_from_the_tournament_being_edited()
        {
            var tournament = ValidTournament();

            using var controller = CreateController();

            await controller.UpdateTournament(tournament);

            Assert.Equal(_beforeUpdate.TournamentId, tournament.TournamentId);
            Assert.Equal(_beforeUpdate.TournamentRoute, tournament.TournamentRoute);
            Assert.Equal(_beforeUpdate.Seasons, tournament.Seasons);
        }

        [Fact]
        public async Task Tournament_notes_are_read_from_form_so_that_HTML_is_allowed()
        {
            var tournament = ValidTournament();

            using var controller = CreateController();

            await controller.UpdateTournament(tournament);

            Assert.Equal("<p>Notes</p>", tournament.TournamentNotes);
        }

        [Fact]
        public async Task Default_over_sets_without_overs_are_removed()
        {
            var tournament = ValidTournament();
            var withOvers = new OverSet { Overs = 6, BallsPerOver = 8 };
            tournament.DefaultOverSets.Add(new OverSet { BallsPerOver = 8 });
            tournament.DefaultOverSets.Add(withOvers);

            using var controller = CreateController();

            await controller.UpdateTournament(tournament);

            Assert.Equal(withOvers, Assert.Single(tournament.DefaultOverSets));
        }

        [Fact]
        public async Task Date_and_start_time_set_known_start_time()
        {
            _form["StartTime"] = "14:30";
            var tournament = ValidTournament();

            using var controller = CreateController();

            await controller.UpdateTournament(tournament);

            Assert.Equal(DateTimeOffset.Parse("2026-06-01").AddHours(14).AddMinutes(30), tournament.StartTime);
            Assert.True(tournament.StartTimeIsKnown);
        }

        [Fact]
        public async Task Date_without_start_time_sets_unknown_start_time_of_11am_on_that_date()
        {
            var tournament = ValidTournament();
            tournament.StartTimeIsKnown = true;

            using var controller = CreateController();

            await controller.UpdateTournament(tournament);

            Assert.Equal(DateTimeOffset.Parse("2026-06-01").AddHours(11), tournament.StartTime);
            Assert.False(tournament.StartTimeIsKnown);
        }

        [Fact]
        public async Task Invalid_start_time_is_a_model_error()
        {
            _form["StartTime"] = "not-a-time";

            using var controller = CreateController();

            var result = await controller.UpdateTournament(ValidTournament());

            Assert.IsType<ViewResult>(result);
            Assert.Contains(controller.ModelState["StartTime"]!.Errors, x => x.ErrorMessage == "Enter a time in 24-hour HH:MM format.");
            VerifyNothingUpdated();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-date")]
        public async Task Missing_or_invalid_date_is_a_model_error(string? date)
        {
            if (date is null) { _form.Remove("TournamentDate"); } else { _form["TournamentDate"] = date; }

            using var controller = CreateController();

            var result = await controller.UpdateTournament(ValidTournament());

            Assert.IsType<ViewResult>(result);
            Assert.Contains(controller.ModelState["TournamentDate"]!.Errors, x => x.ErrorMessage == "Enter a date in YYYY-MM-DD format.");
            VerifyNothingUpdated();
        }

        [Fact]
        public async Task Date_is_validated_for_SQL_Server_and_against_the_first_season()
        {
            using var controller = CreateController();

            await controller.UpdateTournament(ValidTournament());

            var expectedDate = DateTimeOffset.Parse("2026-06-01");
            _matchValidator.Verify(x => x.DateIsValidForSqlServer(expectedDate, controller.ModelState, "TournamentDate", "tournament"), Times.Once);
            _matchValidator.Verify(x => x.DateIsWithinTheSeason(expectedDate, _beforeUpdate.Seasons[0], controller.ModelState, "TournamentDate", "tournament"), Times.Once);
        }

        [Fact]
        public async Task Tournament_location_is_read_from_form()
        {
            var locationId = Guid.NewGuid();
            _form["TournamentLocationId"] = locationId.ToString();
            _form["TournamentLocationName"] = "Example ground";
            var tournament = ValidTournament();

            using var controller = CreateController();
            controller.ModelState.AddModelError("Any", "Any error");

            var result = await controller.UpdateTournament(tournament);

            var model = Assert.IsType<EditTournamentViewModel>(((ViewResult)result).Model);
            Assert.Equal(locationId, tournament.TournamentLocation?.MatchLocationId);
            Assert.Equal(locationId, model.TournamentLocationId);
            Assert.Equal("Example ground", model.TournamentLocationName);
        }

        [Fact]
        public async Task No_tournament_location_in_form_leaves_location_unset()
        {
            var tournament = ValidTournament();

            using var controller = CreateController();

            await controller.UpdateTournament(tournament);

            Assert.Null(tournament.TournamentLocation);
        }

        [Fact]
        public async Task Valid_request_updates_tournament_clears_cache_and_redirects_to_tournament()
        {
            var tournament = ValidTournament();

            using var controller = CreateController();

            var result = await controller.UpdateTournament(tournament);

            _tournamentRepository.Verify(x => x.UpdateTournament(tournament, _currentMember.Key, _currentMember.Name!), Times.Once);
            _cacheClearer.Verify(x => x.InvalidateCacheForTournament(_beforeUpdate, _updatedTournament), Times.Once);
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal(_updatedTournament.TournamentRoute, redirect.Url);
        }

        [Fact]
        public async Task Not_authorised_returns_EditTournament_view_without_updating_tournament()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTournament, false } });

            using var controller = CreateController();

            var result = await controller.UpdateTournament(ValidTournament());

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("EditTournament", view.ViewName);
            VerifyNothingUpdated();
        }

        [Fact]
        public async Task Invalid_ModelState_returns_view_model_with_tournament_date_page_title_authorization_and_breadcrumbs()
        {
            _form["StartTime"] = "14:30";
            var tournament = ValidTournament();

            using var controller = CreateController();
            controller.ModelState.AddModelError("Tournament.TournamentName", "Any error");

            var result = await controller.UpdateTournament(tournament);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<EditTournamentViewModel>(view.Model);
            Assert.Equal("EditTournament", view.ViewName);
            Assert.Equal(tournament, model.Tournament);
            Assert.Equal(DateTimeOffset.Parse("2026-06-01"), model.TournamentDate);
            Assert.Equal(new TimeSpan(14, 30, 0), model.StartTime!.Value.TimeOfDay);
            Assert.Equal("Edit New name tournament, 1 June 2026", model.Metadata.PageTitle);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditTournament]);
            Assert.Equal([Constants.Pages.Tournaments, "New name"], model.Breadcrumbs.Select(x => x.Name).TakeLast(2));
            Assert.Equal(_beforeUpdate.TournamentRoute, model.Breadcrumbs.Last().Url?.ToString());
            VerifyNothingUpdated();
        }
    }
}
