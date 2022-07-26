using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Security;
using Stoolball.Web.Competitions;
using Stoolball.Web.Competitions.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class EditSeasonControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();

        public EditSeasonControllerTests() : base()
        {
        }

        private EditSeasonController CreateController()
        {
            return new EditSeasonController(
                Mock.Of<ILogger<EditSeasonController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _seasonDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Competition>>())
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_season_returns_404()
        {
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<Season?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_season_returns_SeasonViewModel()
        {
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).ReturnsAsync(new Season
            {
                Competition = new Competition
                {
                    CompetitionName = "Example competition",
                    CompetitionRoute = "/competitions/example"
                },
                SeasonRoute = "/competitions/example/1234"
            });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<SeasonViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
