using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class CreateTournamentSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<ITournamentRepository> _tournamentRepository = new();
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<IMatchValidator> _matchValidator = new();
        private readonly Mock<IMatchListingCacheInvalidator> _cacheClearer = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly Dictionary<string, StringValues> _form = new()
        {
            { "Tournament.TournamentNotes", "<p>Notes</p>" },
            { "TournamentDate", "2026-06-01" }
        };
        private readonly Team _team = new() { TeamId = Guid.NewGuid(), TeamName = "Example team", TeamRoute = "/teams/example-team" };
        private readonly Season _season = new()
        {
            SeasonId = Guid.NewGuid(),
            FromYear = 2026,
            UntilYear = 2026,
            EnableTournaments = true,
            Competition = new Competition { CompetitionName = "Example league" }
        };
        private readonly Tournament _createdTournament = new() { TournamentId = Guid.NewGuid(), TournamentRoute = "/tournaments/created-tournament" };

        public CreateTournamentSurfaceControllerTests()
        {
            Request.SetupGet(x => x.Form).Returns(() => new FormCollection(_form));
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).ReturnsAsync(_team);
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).ReturnsAsync(_season);
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _tournamentRepository.Setup(x => x.CreateTournament(It.IsAny<Tournament>(), _currentMember.Key, _currentMember.Name!)).ReturnsAsync(_createdTournament);
            SetupCurrentMember(Mock.Of<IMember>(x => x.Name == "Jo Bloggs"));
            SetupTeamRoute();
        }

        private void SetupTeamRoute() => Request.SetupGet(x => x.Path).Returns(new PathString("/teams/example-team/matches/add/tournament"));
        private void SetupSeasonRoute() => Request.SetupGet(x => x.Path).Returns(new PathString("/competitions/example-league/2026/matches/add/tournament"));

        private CreateTournamentSurfaceController CreateController()
        {
            return new CreateTournamentSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _tournamentRepository.Object,
                _teamDataSource.Object,
                _seasonDataSource.Object,
                _matchValidator.Object,
                _cacheClearer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private static Tournament ValidTournament() => new() { TournamentName = "Example tournament" };

        private async Task<(EditTournamentViewModel model, string? viewName)> CreateTournamentExpectingView(Tournament tournament, Action<CreateTournamentSurfaceController>? arrange = null)
        {
            using var controller = CreateController();
            arrange?.Invoke(controller);

            var result = await controller.CreateTournament(tournament);

            var view = Assert.IsType<ViewResult>(result);
            return (Assert.IsType<EditTournamentViewModel>(view.Model), view.ViewName);
        }

        private void VerifyNothingCreated()
        {
            _tournamentRepository.Verify(x => x.CreateTournament(It.IsAny<Tournament>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _cacheClearer.Verify(x => x.InvalidateCacheForTournament(It.IsAny<Tournament>()), Times.Never);
        }

        [Fact]
        public async Task Null_tournament_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.CreateTournament(null!));
        }

        [Fact]
        public async Task Valid_request_for_team_creates_tournament_with_team_as_organiser_clears_cache_and_redirects()
        {
            var tournament = ValidTournament();

            using var controller = CreateController();

            var result = await controller.CreateTournament(tournament);

            var teamInTournament = Assert.Single(tournament.Teams);
            Assert.Equal(_team, teamInTournament.Team);
            Assert.Equal(TournamentTeamRole.Organiser, teamInTournament.TeamRole);
            _tournamentRepository.Verify(x => x.CreateTournament(tournament, _currentMember.Key, _currentMember.Name!), Times.Once);
            _cacheClearer.Verify(x => x.InvalidateCacheForTournament(_createdTournament), Times.Once);
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal(_createdTournament.TournamentRoute, redirect.Url);
            _seasonDataSource.Verify(x => x.ReadSeasonByRoute(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Valid_request_for_season_creates_tournament_in_season()
        {
            SetupSeasonRoute();
            var tournament = ValidTournament();

            using var controller = CreateController();

            var result = await controller.CreateTournament(tournament);

            Assert.Equal(_season, Assert.Single(tournament.Seasons));
            Assert.Empty(tournament.Teams);
            _tournamentRepository.Verify(x => x.CreateTournament(tournament, _currentMember.Key, _currentMember.Name!), Times.Once);
            Assert.IsType<RedirectResult>(result);
            _teamDataSource.Verify(x => x.ReadTeamByRoute(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Season_without_tournaments_enabled_returns_view_without_creating_tournament()
        {
            SetupSeasonRoute();
            _season.EnableTournaments = false;

            await CreateTournamentExpectingView(ValidTournament());

            VerifyNothingCreated();
        }

        [Fact]
        public async Task Route_for_neither_team_nor_season_returns_view_without_creating_tournament()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/tournaments/add"));

            var (model, _) = await CreateTournamentExpectingView(ValidTournament());

            Assert.Null(model.Team);
            Assert.Null(model.Season);
            VerifyNothingCreated();
        }

        [Fact]
        public async Task Default_over_sets_without_overs_are_removed()
        {
            var tournament = ValidTournament();
            var withOvers = new OverSet { Overs = 6, BallsPerOver = 8 };
            tournament.DefaultOverSets.Add(new OverSet { BallsPerOver = 8 });
            tournament.DefaultOverSets.Add(withOvers);

            using var controller = CreateController();

            await controller.CreateTournament(tournament);

            Assert.Equal(withOvers, Assert.Single(tournament.DefaultOverSets));
        }

        [Fact]
        public async Task Tournament_notes_are_read_from_form_so_that_HTML_is_allowed()
        {
            var tournament = ValidTournament();

            using var controller = CreateController();

            await controller.CreateTournament(tournament);

            Assert.Equal("<p>Notes</p>", tournament.TournamentNotes);
        }

        [Fact]
        public async Task Date_and_start_time_set_known_start_time()
        {
            _form["StartTime"] = "14:30";
            var tournament = ValidTournament();

            using var controller = CreateController();

            await controller.CreateTournament(tournament);

            var expectedDate = DateTimeOffset.Parse("2026-06-01");
            Assert.Equal(expectedDate.AddHours(14).AddMinutes(30), tournament.StartTime);
            Assert.True(tournament.StartTimeIsKnown);
        }

        [Fact]
        public async Task Date_and_start_time_are_added_to_view_model()
        {
            _form["StartTime"] = "14:30";
            var tournament = ValidTournament();

            var (model, _) = await CreateTournamentExpectingView(tournament, controller => controller.ModelState.AddModelError("Any", "Any error"));

            Assert.Equal(DateTimeOffset.Parse("2026-06-01"), model.TournamentDate);
            Assert.Equal(new TimeSpan(14, 30, 0), model.StartTime!.Value.TimeOfDay);
        }

        [Fact]
        public async Task Date_without_start_time_sets_unknown_start_time_of_11am_on_that_date()
        {
            var tournament = ValidTournament();
            tournament.StartTimeIsKnown = true;

            using var controller = CreateController();

            await controller.CreateTournament(tournament);

            Assert.Equal(DateTimeOffset.Parse("2026-06-01").AddHours(11), tournament.StartTime);
            Assert.False(tournament.StartTimeIsKnown);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-date")]
        public async Task Missing_or_invalid_date_is_a_model_error(string? date)
        {
            if (date is null) { _form.Remove("TournamentDate"); } else { _form["TournamentDate"] = date; }

            using var controller = CreateController();

            var result = await controller.CreateTournament(ValidTournament());

            Assert.IsType<ViewResult>(result);
            Assert.Contains(controller.ModelState["TournamentDate"]!.Errors, x => x.ErrorMessage == "Enter a date in YYYY-MM-DD format.");
            VerifyNothingCreated();
        }

        [Fact]
        public async Task Invalid_start_time_is_a_model_error()
        {
            _form["StartTime"] = "not-a-time";

            using var controller = CreateController();

            var result = await controller.CreateTournament(ValidTournament());

            Assert.IsType<ViewResult>(result);
            VerifyNothingCreated();
            Assert.Contains(controller.ModelState["StartTime"]!.Errors, x => x.ErrorMessage == "Enter a time in 24-hour HH:MM format.");
        }

        [Fact]
        public async Task Date_is_validated_for_SQL_Server()
        {
            using var controller = CreateController();

            await controller.CreateTournament(ValidTournament());

            _matchValidator.Verify(x => x.DateIsValidForSqlServer(DateTimeOffset.Parse("2026-06-01"), controller.ModelState, "TournamentDate", "tournament"), Times.Once);
        }

        [Fact]
        public async Task Date_is_validated_against_season_for_season_route()
        {
            SetupSeasonRoute();

            using var controller = CreateController();

            await controller.CreateTournament(ValidTournament());

            _matchValidator.Verify(x => x.DateIsWithinTheSeason(DateTimeOffset.Parse("2026-06-01"), _season, controller.ModelState, "TournamentDate", "tournament"), Times.Once);
        }

        [Fact]
        public async Task Date_is_not_validated_against_season_for_team_route()
        {
            using var controller = CreateController();

            await controller.CreateTournament(ValidTournament());

            _matchValidator.Verify(x => x.DateIsWithinTheSeason(It.IsAny<DateTimeOffset?>(), It.IsAny<Season?>(), It.IsAny<ModelStateDictionary>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Tournament_location_is_read_from_form()
        {
            var locationId = Guid.NewGuid();
            _form["TournamentLocationId"] = locationId.ToString();
            _form["TournamentLocationName"] = "Example ground";
            var tournament = ValidTournament();

            var (model, _) = await CreateTournamentExpectingView(tournament, controller => controller.ModelState.AddModelError("Any", "Any error"));

            Assert.Equal(locationId, tournament.TournamentLocation?.MatchLocationId);
            Assert.Equal(locationId, model.TournamentLocationId);
            Assert.Equal("Example ground", model.TournamentLocationName);
        }

        [Fact]
        public async Task No_tournament_location_in_form_leaves_location_unset()
        {
            var tournament = ValidTournament();

            using var controller = CreateController();

            await controller.CreateTournament(tournament);

            Assert.Null(tournament.TournamentLocation);
        }

        [Fact]
        public async Task Signed_out_member_returns_view_without_creating_tournament()
        {
            HttpContext.Setup(x => x.User).Returns(new ClaimsPrincipal(new ClaimsIdentity()));

            var (model, viewName) = await CreateTournamentExpectingView(ValidTournament());

            Assert.Equal("CreateTournament", viewName);
            Assert.False(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.CreateTournament]);
            VerifyNothingCreated();
        }

        [Fact]
        public async Task Invalid_ModelState_for_team_returns_view_model_with_team_page_title_authorization_and_breadcrumbs()
        {
            var tournament = ValidTournament();

            var (model, viewName) = await CreateTournamentExpectingView(tournament, controller => controller.ModelState.AddModelError("Any", "Any error"));

            Assert.Equal("CreateTournament", viewName);
            Assert.Equal(tournament, model.Tournament);
            Assert.Equal(_team, model.Team);
            Assert.Equal("Add a tournament for Example team", model.Metadata.PageTitle);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.CreateTournament]);
            Assert.Equal(Constants.Pages.Tournaments, model.Breadcrumbs.Last().Name);
            VerifyNothingCreated();
        }

        [Fact]
        public async Task Invalid_ModelState_for_season_returns_view_model_with_season_and_page_title()
        {
            SetupSeasonRoute();

            var (model, _) = await CreateTournamentExpectingView(ValidTournament(), controller => controller.ModelState.AddModelError("Any", "Any error"));

            Assert.Equal(_season, model.Season);
            Assert.Equal($"Add a tournament in the {_season.SeasonFullName()}", model.Metadata.PageTitle);
            VerifyNothingCreated();
        }
    }
}
