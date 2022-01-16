using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Moq;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.UnitTests;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.MatchLocations
{
    public class MatchesForMatchLocationControllerTests : UmbracoBaseTest
    {
        public MatchesForMatchLocationControllerTests()
        {
            Setup();
        }

        private class TestController : MatchesForMatchLocationController
        {
            public TestController(IMatchLocationDataSource matchLocationDataSource, IMatchListingDataSource matchDataSource, IMatchFilterQueryStringParser matchFilterQueryStringParser, UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                Mock.Of<IMatchFilterFactory>(),
                matchLocationDataSource,
                matchDataSource,
                Mock.Of<IAuthorizationPolicy<MatchLocation>>(),
                Mock.Of<IDateTimeFormatter>(),
                matchFilterQueryStringParser,
                Mock.Of<IMatchFilterHumanizer>())
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(new Uri("https://example.org"));

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                ControllerContext = new ControllerContext(context.Object, new RouteData(), this);
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("MatchesForMatchLocation", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_location_returns_404()
        {
            var locationDataSource = new Mock<IMatchLocationDataSource>();
            locationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<MatchLocation>(null));

            var filter = new MatchFilter();
            var matchFilterQueryStringParser = new Mock<IMatchFilterQueryStringParser>();
            matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<NameValueCollection>())).Returns(filter);

            var matchesDataSource = new Mock<IMatchListingDataSource>();
            matchesDataSource.Setup(x => x.ReadMatchListings(filter, MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());

            using (var controller = new TestController(locationDataSource.Object, matchesDataSource.Object, matchFilterQueryStringParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_location_returns_MatchLocationViewModel()
        {
            var locationDataSource = new Mock<IMatchLocationDataSource>();
            locationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).ReturnsAsync(new MatchLocation { MatchLocationId = Guid.NewGuid() });

            var filter = new MatchFilter();
            var matchFilterQueryStringParser = new Mock<IMatchFilterQueryStringParser>();
            matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<NameValueCollection>())).Returns(filter);

            var matchesDataSource = new Mock<IMatchListingDataSource>();
            matchesDataSource.Setup(x => x.ReadMatchListings(filter, MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());

            using (var controller = new TestController(locationDataSource.Object, matchesDataSource.Object, matchFilterQueryStringParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<MatchLocationViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
