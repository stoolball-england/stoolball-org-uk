﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Statistics;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.Statistics;
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
    public class MatchLocationStatisticsControllerTests : UmbracoBaseTest
    {
        public MatchLocationStatisticsControllerTests()
        {
            Setup();
        }

        private class TestController : MatchLocationStatisticsController
        {
            public TestController(IMatchLocationDataSource matchLocationDataSource,
                IBestPerformanceInAMatchStatisticsDataSource bestPerformanceDataSource,
                IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser,
                UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                matchLocationDataSource,
                bestPerformanceDataSource,
                Mock.Of<IBestPlayerTotalStatisticsDataSource>(),
                statisticsFilterQueryStringParser,
                Mock.Of<IStatisticsFilterHumanizer>())
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

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("MatchLocationStatistics", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_location_returns_404()
        {
            var statisticsFilterQueryStringParser = new Mock<IStatisticsFilterQueryStringParser>();
            statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), It.IsAny<NameValueCollection>())).Returns(new StatisticsFilter());
            var locationDataSource = new Mock<IMatchLocationDataSource>();
            locationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<MatchLocation>(null));
            var statisticsDataSource = new Mock<IBestPerformanceInAMatchStatisticsDataSource>();

            using (var controller = new TestController(locationDataSource.Object, statisticsDataSource.Object, statisticsFilterQueryStringParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_location_returns_StatisticsSummaryViewModel()
        {
            var statisticsFilterQueryStringParser = new Mock<IStatisticsFilterQueryStringParser>();
            statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), It.IsAny<NameValueCollection>())).Returns(new StatisticsFilter());
            var locationDataSource = new Mock<IMatchLocationDataSource>();
            locationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).ReturnsAsync(new MatchLocation { MatchLocationId = Guid.NewGuid() });
            var statisticsDataSource = new Mock<IBestPerformanceInAMatchStatisticsDataSource>();
            statisticsDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>(), StatisticsSortOrder.BestFirst)).Returns(Task.FromResult(new StatisticsResult<PlayerInnings>[] { new StatisticsResult<PlayerInnings>() } as IEnumerable<StatisticsResult<PlayerInnings>>));

            using (var controller = new TestController(locationDataSource.Object, statisticsDataSource.Object, statisticsFilterQueryStringParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<StatisticsSummaryViewModel<MatchLocation>>(((ViewResult)result).Model);
            }
        }
    }
}