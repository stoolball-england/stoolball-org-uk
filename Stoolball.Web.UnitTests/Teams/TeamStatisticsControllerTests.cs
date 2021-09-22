using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Statistics;
using Stoolball.Web.Teams;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.Tests.Teams
{
    public class TeamStatisticsControllerTests : UmbracoBaseTest
    {
        public TeamStatisticsControllerTests()
        {
            Setup();
        }

        private class TestController : TeamStatisticsController
        {
            public TestController(ITeamDataSource teamDataSource,
                IBestPerformanceInAMatchStatisticsDataSource bestPerformanceDataSource,
                IInningsStatisticsDataSource inningsStatisticsDataSource,
                IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser,
                UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                teamDataSource,
                bestPerformanceDataSource,
                inningsStatisticsDataSource,
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
                return View("TeamStatistics", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_team_returns_404()
        {
            var teamDataSource = new Mock<ITeamDataSource>();
            teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<Team>(null));
            var statisticsFilterQueryStringParser = new Mock<IStatisticsFilterQueryStringParser>();
            statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), It.IsAny<NameValueCollection>())).Returns(new StatisticsFilter());
            var statisticsDataSource = new Mock<IBestPerformanceInAMatchStatisticsDataSource>();
            var inningsDataSource = new Mock<IInningsStatisticsDataSource>();

            using (var controller = new TestController(teamDataSource.Object, statisticsDataSource.Object, inningsDataSource.Object, statisticsFilterQueryStringParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_team_returns_StatisticsSummaryViewModel()
        {
            var teamDataSource = new Mock<ITeamDataSource>();
            teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).ReturnsAsync(new Team { TeamId = Guid.NewGuid() });
            var statisticsFilterQueryStringParser = new Mock<IStatisticsFilterQueryStringParser>();
            statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), It.IsAny<NameValueCollection>())).Returns(new StatisticsFilter());
            var statisticsDataSource = new Mock<IBestPerformanceInAMatchStatisticsDataSource>();
            statisticsDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>(), StatisticsSortOrder.BestFirst)).Returns(Task.FromResult(new StatisticsResult<PlayerInnings>[] { new StatisticsResult<PlayerInnings>() } as IEnumerable<StatisticsResult<PlayerInnings>>));
            var inningsDataSource = new Mock<IInningsStatisticsDataSource>();

            using (var controller = new TestController(teamDataSource.Object, statisticsDataSource.Object, inningsDataSource.Object, statisticsFilterQueryStringParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<StatisticsSummaryViewModel<Team>>(((ViewResult)result).Model);
            }
        }
    }
}
