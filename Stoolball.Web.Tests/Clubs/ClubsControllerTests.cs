using Moq;
using Stoolball.Umbraco.Data.Clubs;
using Stoolball.Web.Clubs;
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

namespace Stoolball.Web.Tests.Clubs
{
    public class ClubsControllerTests : UmbracoBaseTest
    {
        private class TestController : ClubsController
        {
            public TestController(IClubDataSource clubDataSource)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                null, clubDataSource)
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(new Uri("https://example.org"));

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                ControllerContext = new ControllerContext(context.Object, new RouteData(), this);
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("Clubs", model);
            }
        }

        [Fact]
        public async Task Returns_ClubViewModel()
        {
            var dataSource = new Mock<IClubDataSource>();

            using (var controller = new TestController(dataSource.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<ClubsViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
