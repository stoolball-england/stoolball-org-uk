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
    public class CreateCompetitionControllerTests : UmbracoBaseTest
    {
        public CreateCompetitionControllerTests()
        {
            Setup();
        }

        private CreateCompetitionController CreateController()
        {
            return new CreateCompetitionController(
                Mock.Of<ILogger<CreateCompetitionController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                Mock.Of<IAuthorizationPolicy<Competition>>()
                )
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Returns_CompetitionViewModel()
        {
            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<CompetitionViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
