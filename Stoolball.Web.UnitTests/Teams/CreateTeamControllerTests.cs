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
    public class CreateTeamControllerTests : UmbracoBaseTest
    {
        public CreateTeamControllerTests()
        {
            Setup();
        }

        private CreateTeamController CreateController()
        {
            return new CreateTeamController(
                Mock.Of<ILogger<CreateTeamController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                Mock.Of<IAuthorizationPolicy<Team>>())
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Returns_TeamViewModel()
        {
            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<TeamViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
