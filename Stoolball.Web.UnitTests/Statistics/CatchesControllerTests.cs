using System;
using System.Collections.Generic;
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
    public class CatchesControllerTests : UmbracoBaseTest
    {
        public CatchesControllerTests()
        {
            Setup();
        }

        private class TestController : CatchesController
        {
            public TestController(IStatisticsFilterUrlParser statisticsFilterUrlParser, IPlayerSummaryStatisticsDataSource playerSummaryStatisticsDataSource, IPlayerPerformanceStatisticsDataSource playerPerformanceStatisticsDataSource, UmbracoHelper umbracoHelper, string queryString)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                statisticsFilterUrlParser,
                playerSummaryStatisticsDataSource,
                playerPerformanceStatisticsDataSource,
                Mock.Of<IStatisticsBreadcrumbBuilder>())
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(new Uri("https://example.org"));
                request.SetupGet(x => x.RawUrl).Returns(Stoolball.Constants.Pages.StatisticsUrl + "/bowling-figures");
                request.SetupGet(x => x.QueryString).Returns(HttpUtility.ParseQueryString(queryString));

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                var controllerContext = new Mock<ControllerContext>();
                controllerContext.Setup(p => p.HttpContext).Returns(context.Object);
                controllerContext.Setup(p => p.HttpContext.User).Returns(new GenericPrincipal(new GenericIdentity("test"), null));
                ControllerContext = controllerContext.Object;
            }

            protected override ActionResult CurrentTemplate<T>(T model) => View("Catches", model);
        }

        [Fact]
        public async Task Player_with_no_catches_returns_404()
        {
            var playerId = Guid.NewGuid();
            var playerSummaryStatisticsDataSource = new Mock<IPlayerSummaryStatisticsDataSource>();
            var urlParser = new Mock<IStatisticsFilterUrlParser>();
            urlParser.Setup(x => x.ParseUrl(It.IsAny<Uri>())).Returns(Task.FromResult(new StatisticsFilter { Player = new Player { PlayerId = playerId } }));

            var playerPerformanceStatisticsDataSource = new Mock<IPlayerPerformanceStatisticsDataSource>();
            playerPerformanceStatisticsDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(new List<StatisticsResult<PlayerInnings>>() as IEnumerable<StatisticsResult<PlayerInnings>>));

            using (var controller = new TestController(urlParser.Object, playerSummaryStatisticsDataSource.Object, playerPerformanceStatisticsDataSource.Object, UmbracoHelper, $"player={playerId}"))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Player_with_catches_returns_StatisticsViewModel()
        {
            var playerId = Guid.NewGuid();
            var playerSummaryStatisticsDataSource = new Mock<IPlayerSummaryStatisticsDataSource>();
            playerSummaryStatisticsDataSource.Setup(x => x.ReadFieldingStatistics(It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(new FieldingStatistics()));
            var playerPerformanceStatisticsDataSource = new Mock<IPlayerPerformanceStatisticsDataSource>();
            var urlParser = new Mock<IStatisticsFilterUrlParser>();
            urlParser.Setup(x => x.ParseUrl(It.IsAny<Uri>())).Returns(Task.FromResult(new StatisticsFilter { Player = new Player { PlayerId = playerId } }));

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
            playerPerformanceStatisticsDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(results as IEnumerable<StatisticsResult<PlayerInnings>>));

            using (var controller = new TestController(urlParser.Object, playerSummaryStatisticsDataSource.Object, playerPerformanceStatisticsDataSource.Object, UmbracoHelper, $"player={playerId}"))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<StatisticsViewModel<PlayerInnings>>(((ViewResult)result).Model);
            }
        }
    }
}
