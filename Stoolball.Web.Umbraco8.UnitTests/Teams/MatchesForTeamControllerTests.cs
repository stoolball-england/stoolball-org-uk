﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Moq;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Teams;
using Stoolball.Web.UnitTests;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class MatchesForTeamControllerTests : UmbracoBaseTest
    {
        public MatchesForTeamControllerTests()
        {
            Setup();
        }

        private class TestController : MatchesForTeamController
        {
            public TestController(ITeamDataSource teamDataSource,
                IMatchListingDataSource matchDataSource,
                ICreateMatchSeasonSelector createMatchSeasonSelector,
                IMatchFilterQueryStringParser matchFilterQueryStringParser,
                UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                teamDataSource,
                Mock.Of<IMatchFilterFactory>(),
                matchDataSource,
                Mock.Of<IDateTimeFormatter>(),
                createMatchSeasonSelector,
                Mock.Of<IAuthorizationPolicy<Team>>(),
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
                return View("MatchesForTeam", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_team_returns_404()
        {
            var teamDataSource = new Mock<ITeamDataSource>();
            teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<Team>(null));

            var filter = new MatchFilter();
            var matchFilterQueryStringParser = new Mock<IMatchFilterQueryStringParser>();
            matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<NameValueCollection>())).Returns(filter);

            var matchesDataSource = new Mock<IMatchListingDataSource>();
            matchesDataSource.Setup(x => x.ReadMatchListings(filter, MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());

            var seasonSelector = new Mock<ICreateMatchSeasonSelector>();

            using (var controller = new TestController(teamDataSource.Object, matchesDataSource.Object, seasonSelector.Object, matchFilterQueryStringParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_team_returns_TeamViewModel()
        {
            var teamDataSource = new Mock<ITeamDataSource>();
            teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).ReturnsAsync(new Team { TeamId = Guid.NewGuid() });

            var filter = new MatchFilter();
            var matchFilterQueryStringParser = new Mock<IMatchFilterQueryStringParser>();
            matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<NameValueCollection>())).Returns(filter);

            var matchesDataSource = new Mock<IMatchListingDataSource>();
            matchesDataSource.Setup(x => x.ReadMatchListings(filter, MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());

            var seasonSelector = new Mock<ICreateMatchSeasonSelector>();
            seasonSelector.Setup(x => x.SelectPossibleSeasons(It.IsAny<IEnumerable<TeamInSeason>>(), It.IsAny<MatchType>())).Returns(new List<Season>());

            using (var controller = new TestController(teamDataSource.Object, matchesDataSource.Object, seasonSelector.Object, matchFilterQueryStringParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<TeamViewModel>(((ViewResult)result).Model);
            }
        }
    }
}