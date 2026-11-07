using System.Collections.Generic;
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
        private readonly Mock<IAuthorizationPolicy<Competition>> _authorizationPolicy = new();

        private CreateCompetitionController CreateController()
        {
            return new CreateCompetitionController(
                Mock.Of<ILogger<CreateCompetitionController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _authorizationPolicy.Object
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
        [Fact]
        public async Task Model_has_new_competition_authorization_page_title_and_breadcrumb()
        {
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.CreateCompetition, true } };
            _authorizationPolicy.Setup(x => x.IsAuthorized(It.IsAny<Competition>())).ReturnsAsync(authorization);

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<CompetitionViewModel>(((ViewResult)result).Model);
            Assert.NotNull(model.Competition);
            Assert.Null(model.Competition!.CompetitionId);
            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
            Assert.Equal("Add a competition", model.Metadata.PageTitle);
            Assert.Equal(Constants.Pages.Competitions, model.Breadcrumbs[^1].Name);
        }
    }
}
