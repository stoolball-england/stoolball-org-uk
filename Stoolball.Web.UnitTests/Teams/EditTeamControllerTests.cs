using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Teams;
using Stoolball.Web.Teams.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class EditTeamControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITeamDataSource> _teamDataSource = new();

        public EditTeamControllerTests() : base()
        {
        }

        private EditTeamController CreateController()
        {
            return new EditTeamController(
                Mock.Of<ILogger<EditTeamController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _teamDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Team>>())
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
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).ReturnsAsync(new Team { TeamRoute = "/teams/example" });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<TeamViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
