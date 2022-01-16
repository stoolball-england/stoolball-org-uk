using System;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using Stoolball.Statistics;
using Stoolball.Web.Statistics;
using Stoolball.Web.UnitTests;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Statistics
{
    public class PlayerBowlingControllerTests : UmbracoBaseTest
    {
        public PlayerBowlingControllerTests()
        {
            Setup();
        }

        private class TestController : PlayerBowlingController
        {
            public TestController(IPlayerDataSource playerDataSource, IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser, UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                playerDataSource,
                Mock.Of<IPlayerSummaryStatisticsDataSource>(),
                Mock.Of<IBestPerformanceInAMatchStatisticsDataSource>(),
                statisticsFilterQueryStringParser,
                Mock.Of<IStatisticsFilterHumanizer>()
                )
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
                return View("PlayerBowling", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_player_returns_404()
        {
            var statisticsFilterQueryStringParser = new Mock<IStatisticsFilterQueryStringParser>();
            statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), It.IsAny<NameValueCollection>())).Returns(new StatisticsFilter());
            var dataSource = new Mock<IPlayerDataSource>();
            dataSource.Setup(x => x.ReadPlayerByRoute(It.IsAny<string>())).Returns(Task.FromResult<Player>(null));

            using (var controller = new TestController(dataSource.Object, statisticsFilterQueryStringParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_player_returns_PlayerBowlingViewModel()
        {
            var statisticsFilterQueryStringParser = new Mock<IStatisticsFilterQueryStringParser>();
            statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), It.IsAny<NameValueCollection>())).Returns(new StatisticsFilter());
            var dataSource = new Mock<IPlayerDataSource>();
            dataSource.Setup(x => x.ReadPlayerByRoute(It.IsAny<string>())).Returns(Task.FromResult<Player>(new Player()));

            using (var controller = new TestController(dataSource.Object, statisticsFilterQueryStringParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<PlayerBowlingViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
