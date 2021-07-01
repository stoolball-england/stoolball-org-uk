using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
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
    public class MostRunOutsControllerTests : UmbracoBaseTest
    {
        public MostRunOutsControllerTests()
        {
            Setup();
        }

        private class TestController : MostRunOutsController
        {
            public TestController(IStatisticsFilterUrlParser statisticsFilterUrlParser, IBestPlayerTotalStatisticsDataSource statisticsDataSource, UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                statisticsFilterUrlParser,
                statisticsDataSource,
                Mock.Of<IStatisticsBreadcrumbBuilder>())
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.Url).Returns(new Uri("https://example.org"));
                request.SetupGet(x => x.RawUrl).Returns(Stoolball.Constants.Pages.StatisticsUrl + "/most-run-outs");

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                var controllerContext = new Mock<ControllerContext>();
                controllerContext.Setup(p => p.HttpContext).Returns(context.Object);
                controllerContext.Setup(p => p.HttpContext.User).Returns(new GenericPrincipal(new GenericIdentity("test"), null));
                ControllerContext = controllerContext.Object;
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("MostRunOuts", model);
            }
        }

        [Fact]
        public async Task No_results_returns_404()
        {
            var filter = new StatisticsFilter();
            var statisticsDataSource = new Mock<IBestPlayerTotalStatisticsDataSource>();
            var urlParser = new Mock<IStatisticsFilterUrlParser>();
            urlParser.Setup(x => x.ParseUrl(It.IsAny<Uri>())).Returns(Task.FromResult(filter));

            var playerId = Guid.NewGuid();
            var results = new List<StatisticsResult<BestStatistic>>();
            statisticsDataSource.Setup(x => x.ReadMostRunOuts(filter)).Returns(Task.FromResult(results as IEnumerable<StatisticsResult<BestStatistic>>));

            using (var controller = new TestController(urlParser.Object, statisticsDataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task With_results_returns_StatisticsViewModel()
        {
            var filter = new StatisticsFilter();
            var statisticsDataSource = new Mock<IBestPlayerTotalStatisticsDataSource>();
            var urlParser = new Mock<IStatisticsFilterUrlParser>();
            urlParser.Setup(x => x.ParseUrl(It.IsAny<Uri>())).Returns(Task.FromResult(filter));

            var playerId = Guid.NewGuid();
            var results = new List<StatisticsResult<BestStatistic>> {
                new StatisticsResult<BestStatistic> {
                    Player = new Player {
                        PlayerIdentities = new List<PlayerIdentity>{
                            new PlayerIdentity{
                                PlayerIdentityName = "Example player"
                            }
                        }
                    }
                }
            };
            statisticsDataSource.Setup(x => x.ReadMostRunOuts(filter)).Returns(Task.FromResult(results as IEnumerable<StatisticsResult<BestStatistic>>));

            using (var controller = new TestController(urlParser.Object, statisticsDataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<StatisticsViewModel<BestStatistic>>(((ViewResult)result).Model);
            }
        }
    }
}
