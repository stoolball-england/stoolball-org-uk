using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Teams;
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
    public class MatchesCalendarControllerTests : UmbracoBaseTest
    {
        public MatchesCalendarControllerTests()
        {
            Setup();
        }

        private class TestController : MatchesCalendarController
        {
            public TestController(IMatchListingDataSource matchListingDataSource, IMatchFilterQueryStringParser matchFilterUrlParser, IMatchFilterHumanizer matchFilterHumanizer, UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                Mock.Of<IClubDataSource>(),
                Mock.Of<ITeamDataSource>(),
                Mock.Of<ICompetitionDataSource>(),
                Mock.Of<IMatchLocationDataSource>(),
                matchListingDataSource,
                Mock.Of<ITournamentDataSource>(),
                Mock.Of<IMatchDataSource>(),
                Mock.Of<IDateTimeFormatter>(),
                matchFilterUrlParser,
                matchFilterHumanizer)
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(new Uri("https://example.org"));
                request.SetupGet(x => x.RawUrl).Returns("/matches.ics");
                request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString(string.Empty));

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                var controllerContext = new Mock<ControllerContext>();
                controllerContext.Setup(p => p.HttpContext).Returns(context.Object);
                controllerContext.Setup(p => p.HttpContext.User).Returns(new GenericPrincipal(new GenericIdentity("test"), null));
                ControllerContext = controllerContext.Object;
            }
            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("MatchesCalendar", model);
            }
        }


        [Fact]
        public async Task No_matches_returns_MatchListingViewModel()
        {
            var filter = new MatchFilter();

            var matchDataSource = new Mock<IMatchListingDataSource>();
            matchDataSource.Setup(x => x.ReadMatchListings(filter, MatchSortOrder.LatestUpdateFirst)).ReturnsAsync(new List<MatchListing>());

            var matchFilterQueryStringParser = new Mock<IMatchFilterQueryStringParser>();
            matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<NameValueCollection>())).Returns(filter);
            var matchFilterHumanizer = new Mock<IMatchFilterHumanizer>();

            using (var controller = new TestController(matchDataSource.Object, matchFilterQueryStringParser.Object, matchFilterHumanizer.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<MatchListingViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
