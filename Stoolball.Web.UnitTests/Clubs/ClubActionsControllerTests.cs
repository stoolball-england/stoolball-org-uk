using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Clubs;
using Stoolball.Security;
using Stoolball.Web.Clubs;
using Xunit;

namespace Stoolball.Web.UnitTests.Clubs
{
    public class ClubActionsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IClubDataSource> _clubDataSource = new();

        public ClubActionsControllerTests()
        {
            Setup();
        }
        private ClubActionsController CreateController()
        {
            return new ClubActionsController(
                Mock.Of<ILogger<ClubActionsController>>(),
                Mock.Of<ICompositeViewEngine>(),
                UmbracoContextAccessor.Object,
                _clubDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Club>>());
        }

        [Fact]
        public async Task Route_not_matching_club_returns_404()
        {
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).Returns(Task.FromResult<Club>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_club_returns_ClubViewModel()
        {
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).ReturnsAsync(new Club { ClubRoute = "/clubs/example" });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<ClubViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
