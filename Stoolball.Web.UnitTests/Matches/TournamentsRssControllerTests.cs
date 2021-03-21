using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Web.Matches;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.Tests.Matches
{
    public class TournamentsRssControllerTests : UmbracoBaseTest
    {
        public TournamentsRssControllerTests()
        {
            Setup();
        }

        private class TestController : TournamentsRssController
        {
            public TestController(IMatchListingDataSource matchDataSource, UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                matchDataSource,
                Mock.Of<IDateTimeFormatter>())
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(new Uri("https://example.org"));
                request.SetupGet(x => x.RawUrl).Returns("/tournaments/all.rss");
                request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString(string.Empty));

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                var controllerContext = new Mock<ControllerContext>();
                controllerContext.Setup(p => p.HttpContext).Returns(context.Object);
                controllerContext.Setup(p => p.HttpContext.User).Returns(new GenericPrincipal(new GenericIdentity("test"), null));
                ControllerContext = controllerContext.Object;
            }
        }

        [Fact]
        public async Task No_matches_returns_MatchListingViewModel()
        {
            var matchDataSource = new Mock<IMatchListingDataSource>();
            matchDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), MatchSortOrder.LatestUpdateFirst)).ReturnsAsync(new List<MatchListing>());

            using (var controller = new TestController(matchDataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<MatchListingViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
