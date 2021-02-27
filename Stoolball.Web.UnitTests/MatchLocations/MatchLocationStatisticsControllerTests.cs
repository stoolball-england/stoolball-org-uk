using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using Stoolball.MatchLocations;
using Stoolball.Statistics;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.Statistics;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.Tests.MatchLocations
{
    public class MatchLocationStatisticsControllerTests : UmbracoBaseTest
    {
        public MatchLocationStatisticsControllerTests()
        {
            Setup();
        }

        private class TestController : MatchLocationStatisticsController
        {
            public TestController(IMatchLocationDataSource matchLocationDataSource, IStatisticsDataSource statisticsDataSource, UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                matchLocationDataSource,
                statisticsDataSource)
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
            var locationDataSource = new Mock<IMatchLocationDataSource>();
            locationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<MatchLocation>(null));
            var statisticsDataSource = new Mock<IStatisticsDataSource>();

            using (var controller = new TestController(locationDataSource.Object, statisticsDataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_location_returns_StatisticsViewModel()
        {
            var locationDataSource = new Mock<IMatchLocationDataSource>();
            locationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), false)).ReturnsAsync(new MatchLocation { MatchLocationId = Guid.NewGuid() });
            var statisticsDataSource = new Mock<IStatisticsDataSource>();
            statisticsDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>(), StatisticsSortOrder.BestFirst)).Returns(Task.FromResult(new PlayerInningsResult[] { new PlayerInningsResult() } as IEnumerable<PlayerInningsResult>));

            using (var controller = new TestController(locationDataSource.Object, statisticsDataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<StatisticsViewModel<MatchLocation>>(((ViewResult)result).Model);
            }
        }
    }
}
