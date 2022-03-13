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
    public class PlayerFieldingControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();
        private readonly Mock<IStatisticsFilterQueryStringParser> _statisticsFilterQueryStringParser = new();
        private readonly Mock<IPlayerSummaryStatisticsDataSource> _playerSummaryDataSource = new();
        private readonly Mock<IPlayerPerformanceStatisticsDataSource> _playerPerformanceDataSource = new();

        public PlayerFieldingControllerTests()
        {
            Setup();
        }

        private PlayerFieldingController CreateController()
        {
            return new PlayerFieldingController(
                Mock.Of<ILogger<PlayerFieldingController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _playerDataSource.Object,
                _playerSummaryDataSource.Object,
                _playerPerformanceDataSource.Object,
                _statisticsFilterQueryStringParser.Object,
                Mock.Of<IStatisticsFilterHumanizer>())
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_player_returns_404()
        {
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), It.IsAny<string>())).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(It.IsAny<string>())).Returns(Task.FromResult<Player>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_player_returns_PlayerFieldingViewModel()
        {
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), It.IsAny<string>())).Returns(new StatisticsFilter { Player = new Player() });
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(It.IsAny<string>())).Returns(Task.FromResult<Player>(new Player()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<PlayerFieldingViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
