using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Statistics;
using Stoolball.Web.Statistics;
using Stoolball.Web.Statistics.Models;
using Umbraco.Cms.Core.Security;
using Xunit;

namespace Stoolball.Web.UnitTests.Statistics
{
    public class StatisticsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IBestPerformanceInAMatchStatisticsDataSource> _bestPerformanceDataSource = new();
        private readonly Mock<IBestPlayerTotalStatisticsDataSource> _totalStatisticsDataSource = new();

        public StatisticsControllerTests() : base()
        {
        }

        private StatisticsController CreateController()
        {
            return new StatisticsController(
                Mock.Of<ILogger<StatisticsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                Mock.Of<IMemberManager>(),
                _bestPerformanceDataSource.Object,
                _totalStatisticsDataSource.Object,
                Mock.Of<IStatisticsFilterQueryStringParser>(),
                Mock.Of<IStatisticsFilterHumanizer>())
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Index_returns_StatisticsSummaryViewModel()
        {
            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<StatisticsSummaryViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
