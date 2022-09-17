using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Web.Configuration;
using Stoolball.Web.Teams;
using Stoolball.Web.Teams.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class TeamsMapControllerTests : UmbracoBaseTest
    {
        public TeamsMapControllerTests() : base()
        {
        }

        private TeamsMapController CreateController()
        {
            return new TeamsMapController(
                Mock.Of<ILogger<TeamsMapController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                Mock.Of<IApiKeyProvider>())
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Returns_TeamsViewModel()
        {
            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<TeamsViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
