using System;
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
    public class EditCompetitionControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ICompetitionDataSource> _competitionDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Competition>> _authorizationPolicy = new();

        private EditCompetitionController CreateController()
        {
            return new EditCompetitionController(
                Mock.Of<ILogger<EditCompetitionController>>(),
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
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionRoute = "/competitions/example" });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<CompetitionViewModel>(((ViewResult)result).Model);
            }
        }
        [Fact]
        public async Task Model_has_competition_authorization_page_title_and_breadcrumbs()
        {
            var competition = new Competition { CompetitionName = "Example", CompetitionRoute = "/competitions/example" };
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditCompetition, true } };
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(competition);
            _authorizationPolicy.Setup(x => x.IsAuthorized(competition)).ReturnsAsync(authorization);

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<CompetitionViewModel>(((ViewResult)result).Model);
            Assert.Equal(competition, model.Competition);
            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
            Assert.Equal("Edit Example", model.Metadata.PageTitle);
            Assert.Equal(Constants.Pages.Competitions, model.Breadcrumbs[^2].Name);
            Assert.Equal("Example", model.Breadcrumbs[^1].Name);
            Assert.Equal("/competitions/example", model.Breadcrumbs[^1].Url!.ToString());
        }

        [Fact]
        public async Task Model_has_referrer_from_request_header()
        {
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionRoute = "/competitions/example" });
            Request.Object.Headers["Referer"] = "https://www.stoolball.org.uk/competitions/example";

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<CompetitionViewModel>(((ViewResult)result).Model);
            Assert.Equal(new Uri("https://www.stoolball.org.uk/competitions/example"), model.UrlReferrer);
        }

        [Fact]
        public async Task Model_has_no_referrer_when_request_has_no_referer_header()
        {
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionRoute = "/competitions/example" });

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<CompetitionViewModel>(((ViewResult)result).Model);
            Assert.Null(model.UrlReferrer);
        }
    }
}
