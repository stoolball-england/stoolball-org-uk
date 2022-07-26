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
    public class MostCatchesControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IStatisticsFilterFactory> _statisticsFilterFactory = new();
        private readonly Mock<IStatisticsFilterQueryStringParser> _statisticsFilterQueryStringParser = new();
        private readonly Mock<IBestPlayerTotalStatisticsDataSource> _playerTotalDataSource = new();

        public MostCatchesControllerTests() : base()
        {
        }

        private MostCatchesController CreateController()
        {
            return new MostCatchesController(
                Mock.Of<ILogger<MostCatchesController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _statisticsFilterFactory.Object,
                _playerTotalDataSource.Object,
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
            _playerTotalDataSource.Setup(x => x.ReadMostCatches(appliedFilter)).Returns(Task.FromResult(results as IEnumerable<StatisticsResult<BestStatistic>>));

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
            _playerTotalDataSource.Setup(x => x.ReadMostCatches(appliedFilter)).Returns(Task.FromResult(results as IEnumerable<StatisticsResult<BestStatistic>>));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<StatisticsViewModel<BestStatistic>>(((ViewResult)result).Model);
            }
        }
    }
}
