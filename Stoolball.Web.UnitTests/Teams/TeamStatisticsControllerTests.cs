using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Statistics.Models;
using Stoolball.Web.Teams;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class TeamStatisticsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<IBestPerformanceInAMatchStatisticsDataSource> _bestPerformanceDataSource = new();
        private readonly Mock<IInningsStatisticsDataSource> _inningsStatisticsDataSource = new();
        private readonly Mock<IStatisticsFilterQueryStringParser> _statisticsFilterQueryStringParser = new();
        private readonly Mock<IBestPlayerTotalStatisticsDataSource> _bestTotalDataSource = new();

        public TeamStatisticsControllerTests() : base()
        {
        }

        private TeamStatisticsController CreateController()
        {
            return new TeamStatisticsController(
                Mock.Of<ILogger<TeamStatisticsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _teamDataSource.Object,
                _bestPerformanceDataSource.Object,
                _inningsStatisticsDataSource.Object,
                _bestTotalDataSource.Object,
                _statisticsFilterQueryStringParser.Object,
                Mock.Of<IStatisticsFilterHumanizer>()
                )
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_team_returns_404()
        {
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<Team?>(null));
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), It.IsAny<string>())).Returns(new StatisticsFilter());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_team_returns_StatisticsSummaryViewModel()
        {
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).ReturnsAsync(new Team { TeamId = Guid.NewGuid() });
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), It.IsAny<string>())).Returns(new StatisticsFilter());
            _bestPerformanceDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>(), StatisticsSortOrder.BestFirst)).Returns(Task.FromResult(new StatisticsResult<PlayerInnings>[] { new StatisticsResult<PlayerInnings>() } as IEnumerable<StatisticsResult<PlayerInnings>>));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<StatisticsSummaryViewModel<Team>>(((ViewResult)result).Model);
            }
        }
    }
}
