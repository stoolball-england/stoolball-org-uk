using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Web.Configuration;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.MatchLocations.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.MatchLocations
{
    public class CreateMatchLocationControllerTests : UmbracoBaseTest
    {
        public CreateMatchLocationControllerTests() : base()
        {
        }

        private CreateMatchLocationController CreateController()
        {
            return new CreateMatchLocationController(
                Mock.Of<ILogger<CreateMatchLocationController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                Mock.Of<IApiKeyProvider>(),
                Mock.Of<IAuthorizationPolicy<MatchLocation>>()
                )
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Returns_MatchLocationViewModel()
        {
            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<MatchLocationViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
