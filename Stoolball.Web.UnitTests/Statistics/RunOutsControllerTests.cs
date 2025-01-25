using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Web.Navigation;
using Stoolball.Web.Statistics;
using Stoolball.Web.Statistics.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Statistics
{
    public class RunOutsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IStatisticsFilterFactory> _statisticsFilterFactory = new();
        private readonly Mock<IPlayerSummaryStatisticsDataSource> _playerSummaryStatisticsDataSource = new();
        private readonly Mock<IPlayerPerformanceStatisticsDataSource> _playerPerformanceStatisticsDataSource = new();

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

            _statisticsFilterFactory.Setup(x => x.FromRoute(Request.Object.Path)).Returns(Task.FromResult(defaultFilter));
            _statisticsFilterFactory.Setup(x => x.FromQueryString(Request.Object.QueryString.Value)).Returns(Task.FromResult(appliedFilter));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Player_is_swapped_to_fielder_when_filtering_results()
        {
            var player = new Player
            {
                PlayerIdentities = [
                    new PlayerIdentity{ PlayerIdentityId = Guid.NewGuid() },
                    new PlayerIdentity{ PlayerIdentityId = Guid.NewGuid() }
                ]
            };
            var defaultFilter = new StatisticsFilter { Player = player };
            var appliedFilter = defaultFilter.Clone();

            _statisticsFilterFactory.Setup(x => x.FromRoute(Request.Object.Path)).Returns(Task.FromResult(defaultFilter));
            _statisticsFilterFactory.Setup(x => x.FromQueryString(Request.Object.QueryString.Value)).Returns(Task.FromResult(appliedFilter));
            _playerSummaryStatisticsDataSource.Setup(x => x.ReadFieldingStatistics(It.Is<StatisticsFilter>(x => x != defaultFilter))).Returns(Task.FromResult(new FieldingStatistics { TotalRunOuts = 10 }));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                _playerPerformanceStatisticsDataSource.Verify(x => x.ReadPlayerInnings(
                    It.Is<StatisticsFilter>(filter => filter.Player == null &&
                                            filter.RunOutByPlayerIdentityIds.Count == 2 &&
                                            filter.RunOutByPlayerIdentityIds.Contains(player.PlayerIdentities[0].PlayerIdentityId!.Value) &&
                                            filter.RunOutByPlayerIdentityIds.Contains(player.PlayerIdentities[1].PlayerIdentityId!.Value))), Times.Once);
            }

        }

        [Fact]
        public async Task Paging_total_is_set_from_TotalRunOuts()
        {
            var defaultFilter = new StatisticsFilter { Player = new Player() };
            var appliedFilter = defaultFilter.Clone();

            _statisticsFilterFactory.Setup(x => x.FromRoute(Request.Object.Path)).Returns(Task.FromResult(defaultFilter));
            _statisticsFilterFactory.Setup(x => x.FromQueryString(Request.Object.QueryString.Value)).Returns(Task.FromResult(appliedFilter));
            _playerSummaryStatisticsDataSource.Setup(x => x.ReadFieldingStatistics(It.Is<StatisticsFilter>(x => x != defaultFilter))).Returns(Task.FromResult(new FieldingStatistics { TotalRunOuts = 10 }));

            using (var controller = CreateController())
            {
                var result = await controller.Index();


                var model = ((StatisticsViewModel<PlayerInnings>)((ViewResult)result).Model);

                Assert.Equal(10, model.AppliedFilter.Paging.Total);
            }
        }

        // These tests cover RunOutsController. Most of what it does is in a base class which is tested separately.
    }
}
