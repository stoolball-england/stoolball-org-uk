using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Teams;
using Stoolball.Web.Teams.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class PlayersForTeamControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();

        private PlayersForTeamController CreateController()
        {
            return new PlayersForTeamController(
                Mock.Of<ILogger<PlayersForTeamController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _teamDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Team>>(),
                _playerDataSource.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_team_returns_404()
        {
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Team?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_team_returns_TeamViewModel()
        {
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).ReturnsAsync(new Team { TeamId = Guid.NewGuid() });

            _playerDataSource.Setup(x => x.ReadPlayerIdentities(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(new List<PlayerIdentity>()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<TeamViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
