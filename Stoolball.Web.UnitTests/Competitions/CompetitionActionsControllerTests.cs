using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Security;
using Stoolball.Web.Competitions;
using Stoolball.Web.Competitions.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class CompetitionActionsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ICompetitionDataSource> _competitionDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Competition>> _authorizationPolicy = new();

        private CompetitionActionsController CreateController()
        {
            return new CompetitionActionsController(
                Mock.Of<ILogger<CompetitionActionsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _competitionDataSource.Object,
                _authorizationPolicy.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_competition_returns_404()
        {
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).Returns(Task.FromResult<Competition?>(null));

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
        [Fact]
        public async Task Model_has_competition_and_authorization()
        {
            var competition = new Competition { CompetitionName = "Example", CompetitionRoute = "/competitions/example" };
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditCompetition, true } };
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(competition);
            _authorizationPolicy.Setup(x => x.IsAuthorized(competition)).ReturnsAsync(authorization);
            _authorizationPolicy.Setup(x => x.AuthorizedMemberNames(competition)).ReturnsAsync(new List<string> { "Jo Bloggs" });
            _authorizationPolicy.Setup(x => x.AuthorizedGroupNames(competition)).Returns(new List<string> { "Administrators" });

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<CompetitionViewModel>(((ViewResult)result).Model);
            Assert.Equal(competition, model.Competition);
            Assert.Equal("Example", model.Authorization.AuthorizationFor);
            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
            Assert.Equal(new[] { "Jo Bloggs" }, model.Authorization.AuthorizedMemberNames);
            Assert.Equal(new[] { "Administrators" }, model.Authorization.AuthorizedGroupNames);
        }

        [Fact]
        public async Task Model_has_page_title_and_breadcrumbs()
        {
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionName = "Example", CompetitionRoute = "/competitions/example" });

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<CompetitionViewModel>(((ViewResult)result).Model);
            Assert.Equal("Edit Example", model.Metadata.PageTitle);
            Assert.Equal(Constants.Pages.Competitions, model.Breadcrumbs[^2].Name);
            Assert.Equal("Example", model.Breadcrumbs[^1].Name);
            Assert.Equal("/competitions/example", model.Breadcrumbs[^1].Url!.ToString());
        }

        [Fact]
        public async Task Route_not_matching_competition_does_not_check_authorization()
        {
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).Returns(Task.FromResult<Competition?>(null));

            using var controller = CreateController();

            await controller.Index();

            _authorizationPolicy.Verify(x => x.IsAuthorized(It.IsAny<Competition>()), Times.Never);
        }
    }
}
