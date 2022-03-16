using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Statistics;
using Stoolball.Web.Statistics;
using Stoolball.Web.Statistics.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Statistics
{
    public class EconomyRateControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IStatisticsFilterFactory> _statisticsFilterFactory = new();
        private readonly Mock<IStatisticsFilterQueryStringParser> _statisticsFilterQueryStringParser = new();
        private readonly Mock<IBestPlayerAverageStatisticsDataSource> _bestAverageDataSource = new();

        public EconomyRateControllerTests()
        {
            Setup();
        }

        private EconomyRateController CreateController()
        {
            return new EconomyRateController(
                Mock.Of<ILogger<EconomyRateController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _statisticsFilterFactory.Object,
                _bestAverageDataSource.Object,
                Mock.Of<IStatisticsBreadcrumbBuilder>(),
                _statisticsFilterQueryStringParser.Object,
                Mock.Of<IStatisticsFilterHumanizer>())
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task No_results_returns_StatisticsViewModel()
        {
            var defaultFilter = new StatisticsFilter();
            var appliedFilter = defaultFilter.Clone();

            _statisticsFilterFactory.Setup(x => x.FromRoute(Request.Object.Path)).Returns(Task.FromResult(defaultFilter));
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(defaultFilter, It.IsAny<string>())).Returns(appliedFilter);

            var results = new List<StatisticsResult<BestStatistic>>();
            _bestAverageDataSource.Setup(x => x.ReadBestEconomyRate(appliedFilter)).Returns(Task.FromResult(results as IEnumerable<StatisticsResult<BestStatistic>>));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<StatisticsViewModel<BestStatistic>>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task With_results_returns_StatisticsViewModel()
        {
            var defaultFilter = new StatisticsFilter();
            var appliedFilter = defaultFilter.Clone();

            _statisticsFilterFactory.Setup(x => x.FromRoute(Request.Object.Path)).Returns(Task.FromResult(defaultFilter));
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(defaultFilter, It.IsAny<string>())).Returns(appliedFilter);

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
            _bestAverageDataSource.Setup(x => x.ReadBestEconomyRate(appliedFilter)).Returns(Task.FromResult(results as IEnumerable<StatisticsResult<BestStatistic>>));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<StatisticsViewModel<BestStatistic>>(((ViewResult)result).Model);
            }
        }
    }
}
