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
    public class PlayerBowlingControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();
        private readonly Mock<IStatisticsFilterQueryStringParser> _statisticsFilterQueryStringParser = new();
        private readonly Mock<IPlayerSummaryStatisticsDataSource> _playerSummaryDataSource = new();
        private readonly Mock<IBestPerformanceInAMatchStatisticsDataSource> _bestPerformanceDataSource = new();

        public PlayerBowlingControllerTests()
        {
            Setup();
        }

        private PlayerBowlingController CreateController()
        {
            return new PlayerBowlingController(
                Mock.Of<ILogger<PlayerBowlingController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _playerDataSource.Object,
                _playerSummaryDataSource.Object,
                _bestPerformanceDataSource.Object,
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
        public async Task Route_matching_player_returns_PlayerBowlingViewModel()
        {
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), It.IsAny<string>())).Returns(new StatisticsFilter());
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(It.IsAny<string>())).Returns(Task.FromResult<Player>(new Player()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<PlayerBowlingViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
