using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Security;
using Stoolball.Web.Competitions;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class CompetitionActionsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ICompetitionDataSource> _competitionDataSource = new();

        public CompetitionActionsControllerTests()
        {
            Setup();
        }

        private CompetitionActionsController CreateController()
        {
            return new CompetitionActionsController(
                Mock.Of<ILogger<CompetitionActionsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _competitionDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Competition>>())
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_competition_returns_404()
        {
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).Returns(Task.FromResult<Competition>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_competition_returns_CompetitionViewModel()
        {
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionName = "Example", CompetitionRoute = "/competitions/example" });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<CompetitionViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
