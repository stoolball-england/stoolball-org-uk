using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Navigation;
using Stoolball.Web.Teams;
using Stoolball.Web.Teams.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class DeleteTeamControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchListingDataSource = new();
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Team>> _authorizationPolicy = new();
        private readonly Mock<ITeamBreadcrumbBuilder> _breadcrumbBuilder = new();

        private DeleteTeamController CreateController()
        {
            return new DeleteTeamController(
                Mock.Of<ILogger<DeleteTeamController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _teamDataSource.Object,
                _matchListingDataSource.Object,
                _playerDataSource.Object,
                _authorizationPolicy.Object,
                _breadcrumbBuilder.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private Team SetupTeam()
        {
            var team = new Team { TeamId = Guid.NewGuid(), TeamName = "Example team", TeamRoute = "/teams/example" };
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).ReturnsAsync(team);
            _playerDataSource.Setup(x => x.ReadPlayerIdentities(It.IsAny<PlayerFilter>())).ReturnsAsync(new List<PlayerIdentity>());
            return team;
        }

        private async Task<DeleteTeamViewModel> ReadModel()
        {
            using var controller = CreateController();

            var result = await controller.Index();

            return Assert.IsType<DeleteTeamViewModel>(((ViewResult)result).Model);
        }

        [Fact]
        public async Task Route_not_matching_team_returns_404()
        {
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Team?>(null));

            using var controller = CreateController();

            var result = await controller.Index();

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Route_matching_team_returns_DeleteTeamViewModel_with_team()
        {
            var team = SetupTeam();

            var model = await ReadModel();

            Assert.Equal(team, model.Team);
        }

        [Fact]
        public async Task Total_matches_is_read_for_the_team_including_tournament_matches()
        {
            var team = SetupTeam();
            _matchListingDataSource.Setup(x => x.ReadTotalMatches(It.IsAny<MatchFilter>())).ReturnsAsync(7);

            var model = await ReadModel();

            Assert.Equal(7, model.TotalMatches);
            _matchListingDataSource.Verify(x => x.ReadTotalMatches(It.Is<MatchFilter>(f => f.TeamIds.SequenceEqual(new[] { team.TeamId!.Value }) && f.IncludeTournamentMatches)), Times.Once);
        }

        [Fact]
        public async Task Team_players_are_read_for_the_team_with_one_identity_per_player()
        {
            var team = SetupTeam();
            var identities = new List<PlayerIdentity> { new() { PlayerIdentityName = "Player one" }, new() { PlayerIdentityName = "Player two" } };
            _playerDataSource.Setup(x => x.ReadPlayerIdentities(It.Is<PlayerFilter>(f => f.TeamIds.SequenceEqual(new[] { team.TeamId!.Value })))).ReturnsAsync(identities);

            var model = await ReadModel();

            Assert.Equal(2, model.Team!.Players.Count);
            Assert.Equal(identities[0], Assert.Single(model.Team.Players[0].PlayerIdentities));
            Assert.Equal(identities[1], Assert.Single(model.Team.Players[1].PlayerIdentities));
        }

        [Fact]
        public async Task Confirmation_requires_the_team_name()
        {
            SetupTeam();

            var model = await ReadModel();

            Assert.Equal("Example team", model.ConfirmDeleteRequest.RequiredText);
        }

        [Fact]
        public async Task Model_has_authorization()
        {
            var team = SetupTeam();
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.DeleteTeam, true } };
            _authorizationPolicy.Setup(x => x.IsAuthorized(team)).ReturnsAsync(authorization);

            var model = await ReadModel();

            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
        }

        [Fact]
        public async Task Model_has_page_title_and_breadcrumbs()
        {
            var team = SetupTeam();

            var model = await ReadModel();

            Assert.Equal("Delete Example team", model.Metadata.PageTitle);
            _breadcrumbBuilder.Verify(x => x.BuildBreadcrumbsForTeam(model.Breadcrumbs, team, true), Times.Once);
        }
    }
}
