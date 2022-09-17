using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Clubs;
using Stoolball.Security;
using Stoolball.Web.Clubs;
using Xunit;

namespace Stoolball.Web.UnitTests.Clubs
{
    public class CreateClubControllerTests : UmbracoBaseTest
    {
        public CreateClubControllerTests() : base()
        {
        }

        private CreateClubController CreateController()
        {
            return new CreateClubController(
                Mock.Of<ILogger<CreateClubController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                Mock.Of<IAuthorizationPolicy<Club>>())
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Returns_ClubViewModel()
        {
            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<ClubViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
