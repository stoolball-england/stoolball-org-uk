using Moq;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Teams;
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

namespace Stoolball.Web.Tests.Teams
{
    public class TeamsControllerTests : UmbracoBaseTest
    {
        private class TestController : TeamsController
        {
            public TestController(ITeamListingDataSource teamDataSource, string queryString = "")
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                null, teamDataSource)
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(new Uri("https://example.org"));
                request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString(queryString));

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                ControllerContext = new ControllerContext(context.Object, new RouteData(), this);
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("Teams", model);
            }
        }

        [Fact]
        public async Task Returns_TeamsViewModel()
        {
            var dataSource = new Mock<ITeamListingDataSource>();

            using (var controller = new TestController(dataSource.Object))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<TeamsViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Reads_query_from_querystring_into_view_model()
        {
            var dataSource = new Mock<ITeamListingDataSource>();

            using (var controller = new TestController(dataSource.Object, "q=example"))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.Equal("example", ((TeamsViewModel)((ViewResult)result).Model).TeamQuery.Query);
            }
        }


        [Fact]
        public async Task Reads_query_from_querystring_into_page_title()
        {
            var dataSource = new Mock<ITeamListingDataSource>();

            using (var controller = new TestController(dataSource.Object, "q=example"))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.Contains("example", ((TeamsViewModel)((ViewResult)result).Model).Metadata.PageTitle, StringComparison.Ordinal);
            }
        }
    }
}
