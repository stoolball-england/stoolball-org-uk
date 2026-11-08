using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Routing;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class EditTournamentTeamsSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<ITournamentDataSource> _tournamentDataSource = new();
        private readonly Mock<ITournamentRepository> _tournamentRepository = new();
        private readonly Mock<IMatchListingCacheInvalidator> _cacheClearer = new();
        private readonly Mock<IAuthorizationPolicy<Tournament>> _authorizationPolicy = new();
        private readonly Mock<IDateTimeFormatter> _dateTimeFormatter = new();
        private readonly Mock<IPostSaveRedirector> _postSaveRedirector = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs", UserName = "jo@example.org" };
        private readonly Tournament _beforeUpdate = new()
        {
            TournamentId = Guid.NewGuid(),
            TournamentName = "Example tournament",
            TournamentRoute = "/tournaments/example-tournament",
            StartTime = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero),
            PlayerType = PlayerType.Ladies,
            TournamentLocation = new MatchLocation { MatchLocationId = Guid.NewGuid() }
        };
        private readonly Tournament _updatedTournament = new() { TournamentRoute = "/tournaments/updated-tournament" };
        private readonly RedirectResult _redirect = new("/tournaments/updated-tournament/edit");

        public EditTournamentTeamsSurfaceControllerTests()
        {
            Request.SetupGet(x => x.Form).Returns(new FormCollection(new Dictionary<string, StringValues> { { "UrlReferrer", "https://www.stoolball.org.uk/tournaments/example-tournament" } }));
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).ReturnsAsync(_beforeUpdate);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTournament, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _tournamentRepository.Setup(x => x.UpdateTeams(It.IsAny<Tournament>(), _currentMember.Key, _currentMember.UserName!, _currentMember.Name!)).ReturnsAsync(_updatedTournament);
            _postSaveRedirector.Setup(x => x.WorkOutRedirect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>())).Returns(_redirect);
            _dateTimeFormatter.Setup(x => x.FormatDate(_beforeUpdate.StartTime, false, false, false)).Returns("1 June 2026");
        }

        private EditTournamentTeamsSurfaceController CreateController()
        {
            return new EditTournamentTeamsSurfaceController(
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
                _postSaveRedirector.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private static Tournament PostedTournament() => new()
        {
            MaximumTeamsInTournament = 10,
            Teams = [new TeamInTournament { Team = new Team { TeamId = Guid.NewGuid() }, TeamRole = TournamentTeamRole.Confirmed }]
        };

        private void VerifyNothingUpdated()
        {
            _tournamentRepository.Verify(x => x.UpdateTeams(It.IsAny<Tournament>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _cacheClearer.Verify(x => x.InvalidateCacheForTournament(It.IsAny<Tournament>(), It.IsAny<Tournament>()), Times.Never);
        }

        [Fact]
        public async Task Null_tournament_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.UpdateTeams(null!));
        }

        [Fact]
        public async Task Tournament_details_are_taken_from_the_tournament_being_edited()
        {
            var posted = PostedTournament();
            posted.TournamentName = "Posted name";
            posted.PlayerType = PlayerType.JuniorBoys;

            using var controller = CreateController();

            await controller.UpdateTeams(posted);

            Assert.Equal(_beforeUpdate.TournamentId, posted.TournamentId);
            Assert.Equal(_beforeUpdate.TournamentName, posted.TournamentName);
            Assert.Equal(_beforeUpdate.TournamentRoute, posted.TournamentRoute);
            Assert.Equal(_beforeUpdate.StartTime, posted.StartTime);
            Assert.Equal(_beforeUpdate.PlayerType, posted.PlayerType);
            Assert.Equal(_beforeUpdate.TournamentLocation, posted.TournamentLocation);
        }

        [Fact]
        public async Task Posted_teams_and_maximum_teams_are_saved()
        {
            var posted = PostedTournament();
            var teams = posted.Teams;

            using var controller = CreateController();

            await controller.UpdateTeams(posted);

            _tournamentRepository.Verify(x => x.UpdateTeams(It.Is<Tournament>(t => t == posted && t.MaximumTeamsInTournament == 10 && t.Teams == teams), _currentMember.Key, _currentMember.UserName!, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Valid_request_updates_teams_clears_cache_and_redirects()
        {
            var posted = PostedTournament();

            using var controller = CreateController();

            var result = await controller.UpdateTeams(posted);

            _tournamentRepository.Verify(x => x.UpdateTeams(posted, _currentMember.Key, _currentMember.UserName!, _currentMember.Name!), Times.Once);
            _cacheClearer.Verify(x => x.InvalidateCacheForTournament(_beforeUpdate, _updatedTournament), Times.Once);
            _postSaveRedirector.Verify(x => x.WorkOutRedirect(
                _beforeUpdate.TournamentRoute!,
                _updatedTournament.TournamentRoute!,
                "/edit",
                "https://www.stoolball.org.uk/tournaments/example-tournament",
                null), Times.Once);
            Assert.Equal(_redirect, result);
        }

        [Fact]
        public async Task Errors_other_than_maximum_teams_are_ignored()
        {
            using var controller = CreateController();
            controller.ModelState.AddModelError("Tournament.Teams[0].Team.TeamName", "Any error");
            controller.ModelState.AddModelError("Tournament.TournamentName", "Any error");

            var result = await controller.UpdateTeams(PostedTournament());

            Assert.Equal(_redirect, result);
            Assert.True(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Maximum_teams_error_returns_view_without_updating_teams()
        {
            using var controller = CreateController();
            controller.ModelState.AddModelError("Tournament.MaximumTeamsInTournament", "Any error");

            var result = await controller.UpdateTeams(PostedTournament());

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("EditTournamentTeams", view.ViewName);
            VerifyNothingUpdated();
        }

        [Fact]
        public async Task Not_authorised_returns_EditTournamentTeams_view_without_updating_teams()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTournament, false } });

            using var controller = CreateController();

            var result = await controller.UpdateTeams(PostedTournament());

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("EditTournamentTeams", view.ViewName);
            VerifyNothingUpdated();
        }

        [Fact]
        public async Task Invalid_request_returns_view_model_with_tournament_page_title_authorization_and_breadcrumbs()
        {
            var posted = PostedTournament();

            using var controller = CreateController();
            controller.ModelState.AddModelError("Tournament.MaximumTeamsInTournament", "Any error");

            var result = await controller.UpdateTeams(posted);

            var model = Assert.IsType<EditTournamentViewModel>(((ViewResult)result).Model);
            Assert.Equal(posted, model.Tournament);
            Assert.Equal("Teams in the Example tournament, 1 June 2026", model.Metadata.PageTitle);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditTournament]);
            Assert.Equal([Constants.Pages.Tournaments, "Example tournament"], model.Breadcrumbs.Select(x => x.Name).TakeLast(2));
            Assert.Equal(_beforeUpdate.TournamentRoute, model.Breadcrumbs.Last().Url?.ToString());
            _postSaveRedirector.Verify(x => x.WorkOutRedirect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        }
    }
}
