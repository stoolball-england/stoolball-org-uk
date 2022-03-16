using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Web.Statistics;
using Stoolball.Web.Statistics.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Statistics
{
    public class IndividualScoresControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IStatisticsFilterFactory> _statisticsFilterFactory = new();
        private readonly Mock<IStatisticsFilterQueryStringParser> _statisticsFilterQueryStringParser = new();
        private readonly Mock<IBestPerformanceInAMatchStatisticsDataSource> _bestPerformanceDataSource = new();

        public IndividualScoresControllerTests()
        {
            Setup();
        }

        private IndividualScoresController CreateController()
        {
            return new IndividualScoresController(
                Mock.Of<ILogger<IndividualScoresController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _statisticsFilterFactory.Object,
                _bestPerformanceDataSource.Object,
                Mock.Of<IStatisticsBreadcrumbBuilder>(),
                _statisticsFilterQueryStringParser.Object,
                Mock.Of<IStatisticsFilterHumanizer>())
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Player_with_no_innings_returns_StatisticsViewModel()
        {
            var defaultFilter = new StatisticsFilter();
            var appliedFilter = defaultFilter.Clone();

            _statisticsFilterFactory.Setup(x => x.FromRoute(Request.Object.Path)).Returns(Task.FromResult(defaultFilter));
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(defaultFilter, It.IsAny<string>())).Returns(appliedFilter);

            var results = new List<StatisticsResult<PlayerInnings>>();
            _bestPerformanceDataSource.Setup(x => x.ReadPlayerInnings(appliedFilter, StatisticsSortOrder.BestFirst)).Returns(Task.FromResult(results as IEnumerable<StatisticsResult<PlayerInnings>>));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<StatisticsViewModel<PlayerInnings>>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Player_with_innings_returns_StatisticsViewModel()
        {
            var defaultFilter = new StatisticsFilter();
            var appliedFilter = defaultFilter.Clone();

            _statisticsFilterFactory.Setup(x => x.FromRoute(Request.Object.Path)).Returns(Task.FromResult(defaultFilter));
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(defaultFilter, It.IsAny<string>())).Returns(appliedFilter);

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
            _bestPerformanceDataSource.Setup(x => x.ReadPlayerInnings(appliedFilter, StatisticsSortOrder.BestFirst)).Returns(Task.FromResult(results as IEnumerable<StatisticsResult<PlayerInnings>>));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<StatisticsViewModel<PlayerInnings>>(((ViewResult)result).Model);
            }
        }
    }
}
