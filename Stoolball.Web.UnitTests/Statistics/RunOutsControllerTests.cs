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
using Stoolball.Web.Statistics;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.Tests.Statistics
{
    public class RunOutsControllerTests : UmbracoBaseTest
    {
        internal static readonly Uri _requestUrl = new Uri("https://example.org" + Constants.Pages.StatisticsUrl + "/bowling-figures?querystring=example");

        public RunOutsControllerTests()
        {
            Setup();
        }

        private class TestController : RunOutsController
        {
            public TestController(IStatisticsFilterFactory statisticsFilterFactory,
                IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser,
                IPlayerSummaryStatisticsDataSource playerSummaryStatisticsDataSource,
                IPlayerPerformanceStatisticsDataSource playerPerformanceStatisticsDataSource,
                UmbracoHelper umbracoHelper,
                string queryString)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                statisticsFilterFactory,
                playerSummaryStatisticsDataSource,
                playerPerformanceStatisticsDataSource,
                Mock.Of<IStatisticsBreadcrumbBuilder>(),
                statisticsFilterQueryStringParser,
                Mock.Of<IStatisticsFilterHumanizer>())
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(_requestUrl);
                request.SetupGet(x => x.RawUrl).Returns(_requestUrl.PathAndQuery);
                request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString(queryString));

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                var controllerContext = new Mock<ControllerContext>();
                controllerContext.Setup(p => p.HttpContext).Returns(context.Object);
                controllerContext.Setup(p => p.HttpContext.User).Returns(new GenericPrincipal(new GenericIdentity("test"), null));
                ControllerContext = controllerContext.Object;
            }

            protected override ActionResult CurrentTemplate<T>(T model) => View("RunOuts", model);
        }

        [Fact]
        public async Task Player_with_no_run_outs_returns_StatisticsViewModel()
        {
            var playerId = Guid.NewGuid();
            var defaultFilter = new StatisticsFilter { Player = new Player { PlayerId = playerId } };
            var appliedFilter = defaultFilter.Clone();

            var filterFactory = new Mock<IStatisticsFilterFactory>();
            filterFactory.Setup(x => x.FromRoute(_requestUrl.AbsolutePath)).Returns(Task.FromResult(defaultFilter));

            var queryStringParser = new Mock<IStatisticsFilterQueryStringParser>();
            queryStringParser.Setup(x => x.ParseQueryString(defaultFilter, It.IsAny<NameValueCollection>())).Returns(appliedFilter);

            var playerPerformanceStatisticsDataSource = new Mock<IPlayerPerformanceStatisticsDataSource>();
            playerPerformanceStatisticsDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(new List<StatisticsResult<PlayerInnings>>() as IEnumerable<StatisticsResult<PlayerInnings>>));

            var playerSummaryStatisticsDataSource = new Mock<IPlayerSummaryStatisticsDataSource>();
            playerSummaryStatisticsDataSource.Setup(x => x.ReadFieldingStatistics(appliedFilter)).Returns(Task.FromResult(new FieldingStatistics()));

            using (var controller = new TestController(filterFactory.Object, queryStringParser.Object, playerSummaryStatisticsDataSource.Object, playerPerformanceStatisticsDataSource.Object, UmbracoHelper, $"player={playerId}"))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<StatisticsViewModel<PlayerInnings>>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Player_with_run_outs_returns_StatisticsViewModel()
        {
            var playerId = Guid.NewGuid();
            var defaultFilter = new StatisticsFilter { Player = new Player { PlayerId = playerId } };
            var appliedFilter = defaultFilter.Clone();

            var filterFactory = new Mock<IStatisticsFilterFactory>();
            filterFactory.Setup(x => x.FromRoute(_requestUrl.AbsolutePath)).Returns(Task.FromResult(defaultFilter));

            var queryStringParser = new Mock<IStatisticsFilterQueryStringParser>();
            queryStringParser.Setup(x => x.ParseQueryString(defaultFilter, It.IsAny<NameValueCollection>())).Returns(appliedFilter);

            var results = new List<StatisticsResult<PlayerInnings>> {
                new StatisticsResult<PlayerInnings> {
                    Player = new Player {
                        PlayerIdentities = new List<PlayerIdentity>{
                            new PlayerIdentity{
                                PlayerIdentityName = "Example player"
                            }
                        }
                    }
                }
            };
            var playerPerformanceStatisticsDataSource = new Mock<IPlayerPerformanceStatisticsDataSource>();
            playerPerformanceStatisticsDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(results as IEnumerable<StatisticsResult<PlayerInnings>>));

            var playerSummaryStatisticsDataSource = new Mock<IPlayerSummaryStatisticsDataSource>();
            playerSummaryStatisticsDataSource.Setup(x => x.ReadFieldingStatistics(appliedFilter)).Returns(Task.FromResult(new FieldingStatistics()));

            using (var controller = new TestController(filterFactory.Object, queryStringParser.Object, playerSummaryStatisticsDataSource.Object, playerPerformanceStatisticsDataSource.Object, UmbracoHelper, $"player={playerId}"))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<StatisticsViewModel<PlayerInnings>>(((ViewResult)result).Model);
            }
        }
    }
}
