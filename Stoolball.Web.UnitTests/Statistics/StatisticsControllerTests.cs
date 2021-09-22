using System;
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
    public class StatisticsControllerTests : UmbracoBaseTest
    {
        public StatisticsControllerTests()
        {
            Setup();
        }

        private class TestController : StatisticsController
        {
            public TestController(IBestPerformanceInAMatchStatisticsDataSource bestPerformanceDataSource, IBestPlayerTotalStatisticsDataSource totalStatisticsDataSource, UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                bestPerformanceDataSource,
                totalStatisticsDataSource,
                Mock.Of<IStatisticsFilterQueryStringParser>(),
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
                return View("Statistics", model);
            }
        }

        [Fact]
        public async Task Index_returns_StatisticsSummaryViewModel()
        {
            var bestPerformanceDataSource = new Mock<IBestPerformanceInAMatchStatisticsDataSource>();
            var totalStatisticsDataSource = new Mock<IBestPlayerTotalStatisticsDataSource>();

            using (var controller = new TestController(bestPerformanceDataSource.Object, totalStatisticsDataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<StatisticsSummaryViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
