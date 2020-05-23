using Moq;
using Stoolball.MatchLocations;
using Stoolball.Umbraco.Data.MatchLocations;
using Stoolball.Web.Clubs;
using Stoolball.Web.Configuration;
using Stoolball.Web.MatchLocations;
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

namespace Stoolball.Web.Tests.MatchLocations
{
    public class EditMatchLocationControllerTests : UmbracoBaseTest
    {
        private class TestController : EditMatchLocationController
        {
            public TestController(IMatchLocationDataSource matchLocationDataSource)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                null, matchLocationDataSource,
                Mock.Of<IApiKeyProvider>())
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(new Uri("https://example.org"));

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                ControllerContext = new ControllerContext(context.Object, new RouteData(), this);
            }

            protected override bool IsAuthorized(MatchLocationViewModel model)
            {
                return true;
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("EditMatchLocation", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_location_returns_404()
        {
            var dataSource = new Mock<IMatchLocationDataSource>();
            dataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<MatchLocation>(null));

            using (var controller = new TestController(dataSource.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_location_returns_MatchLocationViewModel()
        {
            var dataSource = new Mock<IMatchLocationDataSource>();
            dataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).ReturnsAsync(new MatchLocation());

            using (var controller = new TestController(dataSource.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<MatchLocationViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
