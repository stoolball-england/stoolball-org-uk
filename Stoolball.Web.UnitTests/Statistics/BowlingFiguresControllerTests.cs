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
    public class BowlingFiguresControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IStatisticsFilterFactory> _statisticsFilterFactory = new();
        private readonly Mock<IStatisticsFilterQueryStringParser> _statisticsFilterQueryStringParser = new();
        private readonly Mock<IBestPerformanceInAMatchStatisticsDataSource> _bestPerformanceDataSource = new();

        public BowlingFiguresControllerTests()
        {
            Setup();
        }

        private BowlingFiguresController CreateController()
        {
            return new BowlingFiguresController(
                Mock.Of<ILogger<BowlingFiguresController>>(),
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
        public async Task Player_with_no_bowling_returns_StatisticsViewModel()
        {
            var defaultFilter = new StatisticsFilter();
            var appliedFilter = defaultFilter.Clone();

            _statisticsFilterFactory.Setup(x => x.FromRoute(Request.Object.Path)).Returns(Task.FromResult(defaultFilter));
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(defaultFilter, It.IsAny<string>())).Returns(appliedFilter);

            var results = new List<StatisticsResult<BowlingFigures>>();
            _bestPerformanceDataSource.Setup(x => x.ReadBowlingFigures(appliedFilter, StatisticsSortOrder.BestFirst)).Returns(Task.FromResult(results as IEnumerable<StatisticsResult<BowlingFigures>>));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<StatisticsViewModel<BowlingFigures>>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Player_with_bowling_returns_StatisticsViewModel()
        {
            var defaultFilter = new StatisticsFilter();
            var appliedFilter = defaultFilter.Clone();

            _statisticsFilterFactory.Setup(x => x.FromRoute(Request.Object.Path)).Returns(Task.FromResult(defaultFilter));
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(defaultFilter, It.IsAny<string>())).Returns(appliedFilter);

            var results = new List<StatisticsResult<BowlingFigures>> {
                new StatisticsResult<BowlingFigures> {
                    Player = new Player {
                        PlayerIdentities = new List<PlayerIdentity>{
                            new PlayerIdentity{
                                PlayerIdentityName = "Example player"
                            }
                        }
                    }
                }
            };
            _bestPerformanceDataSource.Setup(x => x.ReadBowlingFigures(appliedFilter, StatisticsSortOrder.BestFirst)).Returns(Task.FromResult(results as IEnumerable<StatisticsResult<BowlingFigures>>));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<StatisticsViewModel<BowlingFigures>>(((ViewResult)result).Model);
            }
        }
    }
}
