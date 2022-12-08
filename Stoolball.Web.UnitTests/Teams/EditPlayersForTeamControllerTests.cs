using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Navigation;
using Stoolball.Web.Teams;
using Stoolball.Web.Teams.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class EditPlayersForTeamControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Team>> _authorizationPolicy = new();
        private readonly Mock<ITeamBreadcrumbBuilder> _breadcrumbBuilder = new();

        private EditPlayersForTeamController CreateController()
        {
            return new EditPlayersForTeamController(
                Mock.Of<ILogger<EditPlayersForTeamController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _teamDataSource.Object,
                _authorizationPolicy.Object,
                _playerDataSource.Object,
                _breadcrumbBuilder.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private static Team CreateTeam()
        {
            return new Team
            {
                TeamId = Guid.NewGuid(),
                TeamName = "Example team",
                TeamRoute = "/teams/example-team"
            };
        }

        private void SetupMocks(Team team)
        {
            var players = new List<PlayerIdentity> {
                new PlayerIdentity
                {
                    PlayerIdentityId = Guid.NewGuid(),
                    PlayerIdentityName = "John Smith",
                    Player = new Player
                    {
                        PlayerId = Guid.NewGuid()
                    }
                }
            };
            _teamDataSource.Setup(x => x.ReadTeamByRoute(Request.Object.Path, true)).ReturnsAsync(team);
            _playerDataSource.Setup(x => x.ReadPlayerIdentities(It.Is<PlayerFilter>(x => x.TeamIds.Count == 1 && x.TeamIds.Contains(team.TeamId!.Value)))).Returns(Task.FromResult(players));
            _authorizationPolicy.Setup(x => x.IsAuthorized(team)).Returns(Task.FromResult(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTeam, true } }));
        }

        [Fact]
        public async Task Route_not_matching_team_returns_404()
        {
            _teamDataSource.Setup(x => x.ReadTeamByRoute(Request.Object.Path, true)).Returns(Task.FromResult<Team?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_team_sets_authorization()
        {
            var team = CreateTeam();
            SetupMocks(team);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (TeamViewModel)((ViewResult)result).Model;

                Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditTeam]);
            }
        }

        [Fact]
        public async Task Route_matching_team_returns_player_identities()
        {
            var team = CreateTeam();
            SetupMocks(team);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (TeamViewModel)((ViewResult)result).Model;

                Assert.Single(model.PlayerIdentities);
            }
        }

        [Fact]
        public async Task Route_matching_team_sets_page_title()
        {
            var team = CreateTeam();
            SetupMocks(team);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (TeamViewModel)((ViewResult)result).Model;

                Assert.Equal($"Edit players for {team.TeamName} stoolball team", model.Metadata.PageTitle);
            }
        }

        [Fact]
        public async Task Route_matching_team_sets_breadcrumbs()
        {
            var team = CreateTeam();
            SetupMocks(team);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = (TeamViewModel)((ViewResult)result).Model;

                _breadcrumbBuilder.Verify(x => x.BuildBreadcrumbs(model.Breadcrumbs, model.Team!, true), Times.Once());
            }
        }
    }
}
