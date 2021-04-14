using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using Stoolball.Clubs;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Web.Clubs;
using Stoolball.Web.Statistics;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.Tests.Clubs
{
    public class ClubStatisticsControllerTests : UmbracoBaseTest
    {
        public ClubStatisticsControllerTests()
        {
            Setup();
        }

        private class TestController : ClubStatisticsController
        {
            public TestController(IClubDataSource clubDataSource, IBestPerformanceInAMatchStatisticsDataSource statisticsDataSource, IInningsStatisticsDataSource inningsStatisticsDataSource, UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                clubDataSource,
                statisticsDataSource,
                inningsStatisticsDataSource)
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
                return View("ClubStatistics", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_club_returns_404()
        {
            var clubDataSource = new Mock<IClubDataSource>();
            clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).Returns(Task.FromResult<Club>(null));
            var statisticsDataSource = new Mock<IBestPerformanceInAMatchStatisticsDataSource>();
            var inningsDataSource = new Mock<IInningsStatisticsDataSource>();

            using (var controller = new TestController(clubDataSource.Object, statisticsDataSource.Object, inningsDataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_club_returns_StatisticsSummaryViewModel()
        {
            var clubDataSource = new Mock<IClubDataSource>();
            clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).ReturnsAsync(new Club { ClubId = Guid.NewGuid() });
            var statisticsDataSource = new Mock<IBestPerformanceInAMatchStatisticsDataSource>();
            statisticsDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>(), StatisticsSortOrder.BestFirst)).Returns(Task.FromResult(new StatisticsResult<PlayerInnings>[] { new StatisticsResult<PlayerInnings>() } as IEnumerable<StatisticsResult<PlayerInnings>>));
            var inningsDataSource = new Mock<IInningsStatisticsDataSource>();

            using (var controller = new TestController(clubDataSource.Object, statisticsDataSource.Object, inningsDataSource.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<StatisticsSummaryViewModel<Club>>(((ViewResult)result).Model);
            }
        }
    }
}
