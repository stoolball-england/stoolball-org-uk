using Moq;
using Stoolball.MatchLocations;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Umbraco.Data.MatchLocations;
using Stoolball.Web.MatchLocations;
using System;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.Tests.MatchLocations
{
    public class DeleteMatchLocationControllerTests : UmbracoBaseTest
    {
        private class TestController : DeleteMatchLocationController
        {
            public TestController(IMatchLocationDataSource matchLocationDataSource)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                null, matchLocationDataSource, Mock.Of<IMatchDataSource>())
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
            protected override bool IsAuthorized(DeleteMatchLocationViewModel model)
            {
                return true;
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("DeleteMatchLocation", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_location_returns_404()
        {
            var dataSource = new Mock<IMatchLocationDataSource>();
            dataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<MatchLocation>(null));

            using (var controller = new TestController(dataSource.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_location_returns_DeleteMatchLocationViewModel()
        {
            var dataSource = new Mock<IMatchLocationDataSource>();
            dataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), true)).ReturnsAsync(new MatchLocation { MatchLocationId = Guid.NewGuid() });

            using (var controller = new TestController(dataSource.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<DeleteMatchLocationViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
