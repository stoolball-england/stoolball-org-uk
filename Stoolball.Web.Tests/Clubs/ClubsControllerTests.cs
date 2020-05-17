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
            public TestController(IClubDataSource clubDataSource, string queryString = "")
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
                request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString(queryString));

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                ControllerContext = new ControllerContext(context.Object, new RouteData(), this);
            }

            protected override bool IsAuthorized()
            {
                return true;
            }
            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("Clubs", model);
            }
        }

        [Fact]
        public async Task Returns_ClubsViewModel()
        {
            var dataSource = new Mock<IClubDataSource>();

            using (var controller = new TestController(dataSource.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<ClubsViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Reads_query_from_querystring_into_view_model()
        {
            var dataSource = new Mock<IClubDataSource>();

            using (var controller = new TestController(dataSource.Object, "q=example"))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.Equal("example", ((ClubsViewModel)((ViewResult)result).Model).ClubQuery.Query);
            }
        }


        [Fact]
        public async Task Reads_query_from_querystring_into_page_title()
        {
            var dataSource = new Mock<IClubDataSource>();

            using (var controller = new TestController(dataSource.Object, "q=example"))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.Contains("example", ((ClubsViewModel)((ViewResult)result).Model).Metadata.PageTitle, StringComparison.Ordinal);
            }
        }
    }
}
