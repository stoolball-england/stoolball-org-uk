using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Web.Competitions;
using Stoolball.Web.Statistics;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.Tests.Competitions
{
    public class CompetitionStatisticsControllerTests : UmbracoBaseTest
    {
        public CompetitionStatisticsControllerTests()
        {
            Setup();
        }

        private class TestController : CompetitionStatisticsController
        {
            public TestController(ICompetitionDataSource competitionDataSource,
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
                competitionDataSource,
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
                return View("CompetitionStatistics", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_competition_returns_404()
        {
            var competitionDataSource = new Mock<ICompetitionDataSource>();
            competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).Returns(Task.FromResult<Competition>(null));
            var statisticsFilterQueryStringParser = new Mock<IStatisticsFilterQueryStringParser>();
            statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), It.IsAny<NameValueCollection>())).Returns(new StatisticsFilter());
            var statisticsDataSource = new Mock<IBestPerformanceInAMatchStatisticsDataSource>();

            using (var controller = new TestController(competitionDataSource.Object, statisticsDataSource.Object, statisticsFilterQueryStringParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_competition_returns_StatisticsSummaryViewModel()
        {
            var statisticsFilterQueryStringParser = new Mock<IStatisticsFilterQueryStringParser>();
            statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), It.IsAny<NameValueCollection>())).Returns(new StatisticsFilter());
            var competitionDataSource = new Mock<ICompetitionDataSource>();
            competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionId = Guid.NewGuid() });
            var statisticsDataSource = new Mock<IBestPerformanceInAMatchStatisticsDataSource>();
            statisticsDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>(), StatisticsSortOrder.BestFirst)).Returns(Task.FromResult(new StatisticsResult<PlayerInnings>[] { new StatisticsResult<PlayerInnings>() } as IEnumerable<StatisticsResult<PlayerInnings>>));

            using (var controller = new TestController(competitionDataSource.Object, statisticsDataSource.Object, statisticsFilterQueryStringParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<StatisticsSummaryViewModel<Competition>>(((ViewResult)result).Model);
            }
        }
    }
}
