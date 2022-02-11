using System;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Web.Competitions;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class DeleteSeasonControllerTests : UmbracoBaseTest
    {
        public DeleteSeasonControllerTests()
        {
            Setup();
        }

        private class TestController : DeleteSeasonController
        {
            public TestController(ISeasonDataSource seasonDataSource, UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                seasonDataSource,
                Mock.Of<IMatchListingDataSource>(),
                Mock.Of<IAuthorizationPolicy<Competition>>())
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(new Uri("https://example.org"));

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                var controllerContext = new Mock<ControllerContext>();
                controllerContext.Setup(p => p.HttpContext).Returns(context.Object);
                controllerContext.Setup(p => p.HttpContext.User).Returns(new GenericPrincipal(new GenericIdentity("test"), null));
                ControllerContext = controllerContext.Object;
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("DeleteSeason", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_season_returns_404()
        {
            var dataSource = new Mock<ISeasonDataSource>();
            dataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Season>(null));

            using (var controller = new TestController(dataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_season_returns_DeleteSeasonViewModel()
        {
            var dataSource = new Mock<ISeasonDataSource>();
            dataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).ReturnsAsync(new Season
            {
                SeasonId = Guid.NewGuid(),
                Competition = new Competition { CompetitionName = "Example", CompetitionRoute = "/competitions/example" },
                SeasonRoute = "/competitions/example/1234"
            });

            using (var controller = new TestController(dataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<DeleteSeasonViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
