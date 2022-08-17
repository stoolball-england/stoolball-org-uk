using System;
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
    public class RunOutsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IStatisticsFilterFactory> _statisticsFilterFactory = new();
        private readonly Mock<IStatisticsFilterQueryStringParser> _statisticsFilterQueryStringParser = new();
        private readonly Mock<IPlayerSummaryStatisticsDataSource> _playerSummaryStatisticsDataSource = new();
        private readonly Mock<IPlayerPerformanceStatisticsDataSource> _playerPerformanceStatisticsDataSource = new();

        public RunOutsControllerTests()
        {
            Setup();
        }

        private RunOutsController CreateController()
        {
            return new RunOutsController(
                Mock.Of<ILogger<RunOutsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _statisticsFilterFactory.Object,
                _playerSummaryStatisticsDataSource.Object,
                _playerPerformanceStatisticsDataSource.Object,
                Mock.Of<IStatisticsBreadcrumbBuilder>(),
                _statisticsFilterQueryStringParser.Object,
                Mock.Of<IStatisticsFilterHumanizer>())
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Player_not_found_returns_404()
        {
            var defaultFilter = new StatisticsFilter { Player = null };
            var appliedFilter = defaultFilter.Clone();

            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(defaultFilter, It.IsAny<string>())).Returns(appliedFilter);
            _statisticsFilterFactory.Setup(x => x.FromRoute(Request.Object.Path)).Returns(Task.FromResult(defaultFilter));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Player_with_no_run_outs_returns_StatisticsViewModel()
        {
            var playerId = Guid.NewGuid();
            var defaultFilter = new StatisticsFilter { Player = new Player { PlayerId = playerId } };
            var appliedFilter = defaultFilter.Clone();

            _statisticsFilterFactory.Setup(x => x.FromRoute(Request.Object.Path)).Returns(Task.FromResult(defaultFilter));
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(defaultFilter, It.IsAny<string>())).Returns(appliedFilter);
            _playerPerformanceStatisticsDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(new List<StatisticsResult<PlayerInnings>>() as IEnumerable<StatisticsResult<PlayerInnings>>));
            _playerSummaryStatisticsDataSource.Setup(x => x.ReadFieldingStatistics(appliedFilter)).Returns(Task.FromResult(new FieldingStatistics()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<StatisticsViewModel<PlayerInnings>>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Player_with_run_outs_returns_StatisticsViewModel()
        {
            var playerId = Guid.NewGuid();
            var defaultFilter = new StatisticsFilter { Player = new Player { PlayerId = playerId } };
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
            _playerPerformanceStatisticsDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>())).Returns(Task.FromResult(results as IEnumerable<StatisticsResult<PlayerInnings>>));
            _playerSummaryStatisticsDataSource.Setup(x => x.ReadFieldingStatistics(appliedFilter)).Returns(Task.FromResult(new FieldingStatistics()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<StatisticsViewModel<PlayerInnings>>(((ViewResult)result).Model);
            }
        }
    }
}
