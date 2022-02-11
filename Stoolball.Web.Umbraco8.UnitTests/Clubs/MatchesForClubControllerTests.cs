using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Moq;
using Stoolball.Clubs;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Web.Clubs;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Clubs
{
    public class MatchesForClubControllerTests : UmbracoBaseTest
    {
        public MatchesForClubControllerTests()
        {
            Setup();
        }

        private class TestController : MatchesForClubController
        {
            public TestController(IClubDataSource clubDataSource, IMatchListingDataSource matchDataSource, IMatchFilterQueryStringParser matchFilterQueryStringParser, UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                clubDataSource,
                matchDataSource,
                Mock.Of<IDateTimeFormatter>(),
                Mock.Of<IMatchFilterFactory>(),
                Mock.Of<IAuthorizationPolicy<Club>>(),
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
                return View("MatchesForClub", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_club_returns_404()
        {
            var clubDataSource = new Mock<IClubDataSource>();
            clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).Returns(Task.FromResult<Club>(null));

            var filter = new MatchFilter();
            var matchFilterQueryStringParser = new Mock<IMatchFilterQueryStringParser>();
            matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<NameValueCollection>())).Returns(filter);

            var matchesDataSource = new Mock<IMatchListingDataSource>();
            matchesDataSource.Setup(x => x.ReadMatchListings(filter, MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());

            using (var controller = new TestController(clubDataSource.Object, matchesDataSource.Object, matchFilterQueryStringParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_club_returns_ClubViewModel()
        {
            var clubDataSource = new Mock<IClubDataSource>();
            clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).ReturnsAsync(new Club());

            var filter = new MatchFilter();
            var matchFilterQueryStringParser = new Mock<IMatchFilterQueryStringParser>();
            matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<NameValueCollection>())).Returns(filter);

            var matchesDataSource = new Mock<IMatchListingDataSource>();
            matchesDataSource.Setup(x => x.ReadMatchListings(filter, MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());

            using (var controller = new TestController(clubDataSource.Object, matchesDataSource.Object, matchFilterQueryStringParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<ClubViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
