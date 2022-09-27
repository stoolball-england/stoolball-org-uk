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
    public class PlayerOfTheMatchControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IStatisticsFilterFactory> _statisticsFilterFactory = new();
        private readonly Mock<IStatisticsFilterQueryStringParser> _statisticsFilterQueryStringParser = new();
        private readonly Mock<IBestPerformanceInAMatchStatisticsDataSource> _bestPerformanceDataSource = new();

        public PlayerOfTheMatchControllerTests() : base()
        {
        }

        private PlayerOfTheMatchController CreateController()
        {
            return new PlayerOfTheMatchController(
                Mock.Of<ILogger<PlayerOfTheMatchController>>(),
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
        public async Task Player_with_no_performances_returns_StatisticsViewModel()
        {
            var defaultFilter = new StatisticsFilter { PlayerOfTheMatch = true };
            var appliedFilter = defaultFilter.Clone();

            _statisticsFilterFactory.Setup(x => x.FromRoute(Request.Object.Path)).Returns(Task.FromResult(defaultFilter));
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(defaultFilter, It.IsAny<string>())).Returns(appliedFilter);

            var results = new List<StatisticsResult<PlayerIdentityPerformance>>();
            _bestPerformanceDataSource.Setup(x => x.ReadPlayerIdentityPerformances(appliedFilter)).Returns(Task.FromResult(results as IEnumerable<StatisticsResult<PlayerIdentityPerformance>>));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<StatisticsViewModel<PlayerIdentityPerformance>>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Player_with_performances_returns_StatisticsViewModel()
        {
            var defaultFilter = new StatisticsFilter { PlayerOfTheMatch = true };
            var appliedFilter = defaultFilter.Clone();

            _statisticsFilterFactory.Setup(x => x.FromRoute(Request.Object.Path)).Returns(Task.FromResult(defaultFilter));
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(defaultFilter, It.IsAny<string>())).Returns(appliedFilter);

            var results = new List<StatisticsResult<PlayerIdentityPerformance>> {
                new StatisticsResult<PlayerIdentityPerformance> {
                    Player = new Player {
                        PlayerIdentities = new List<PlayerIdentity>{
                            new PlayerIdentity{
                                PlayerIdentityName = "Example player"
                            }
                        }
                    }
                }
            };
            _bestPerformanceDataSource.Setup(x => x.ReadPlayerIdentityPerformances(appliedFilter)).Returns(Task.FromResult(results as IEnumerable<StatisticsResult<PlayerIdentityPerformance>>));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<StatisticsViewModel<PlayerIdentityPerformance>>(((ViewResult)result).Model);
            }
        }
    }
}
