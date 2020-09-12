using Moq;
using Stoolball.Competitions;
using Stoolball.Security;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Web.Competitions;
using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.Tests.Competitions
{
    public class EditSeasonPointsControllerTests : UmbracoBaseTest
    {
        public EditSeasonPointsControllerTests()
        {
            Setup();
        }

        private class TestController : EditSeasonPointsController
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
                Mock.Of<IAuthorizationPolicy<Competition>>())
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(new Uri("https://example.org"));

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                ControllerContext = new ControllerContext(context.Object, new RouteData(), this);
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("EditSeasonPoints", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_season_returns_404()
        {
            var dataSource = new Mock<ISeasonDataSource>();
            dataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<Season>(null));

            using (var controller = new TestController(dataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_season_returns_SeasonViewModel()
        {
            var dataSource = new Mock<ISeasonDataSource>();
            dataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).ReturnsAsync(new Season { SeasonId = Guid.NewGuid() });

            using (var controller = new TestController(dataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<SeasonViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
