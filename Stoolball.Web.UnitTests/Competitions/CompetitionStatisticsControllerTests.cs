using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Web.Competitions;
using Stoolball.Web.Statistics.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class CompetitionStatisticsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ICompetitionDataSource> _competitionDataSource = new();
        private readonly Mock<IStatisticsFilterQueryStringParser> _statisticsFilterQueryStringParser = new();
        private readonly Mock<IBestPerformanceInAMatchStatisticsDataSource> _bestPerformanceDataSource = new();
        private readonly Mock<IBestPlayerTotalStatisticsDataSource> _bestTotalDataSource = new();

        private CompetitionStatisticsController CreateController()
        {
            return new CompetitionStatisticsController(
                Mock.Of<ILogger<CompetitionStatisticsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _competitionDataSource.Object,
                _bestPerformanceDataSource.Object,
                _bestTotalDataSource.Object,
                _statisticsFilterQueryStringParser.Object,
                Mock.Of<IStatisticsFilterHumanizer>()
                )
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_competition_returns_404()
        {
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).Returns(Task.FromResult<Competition?>(null));
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), It.IsAny<string>())).Returns(new StatisticsFilter());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_competition_returns_StatisticsSummaryViewModel()
        {
            _statisticsFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<StatisticsFilter>(), It.IsAny<string>())).Returns(new StatisticsFilter());
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionId = Guid.NewGuid() });
            _bestPerformanceDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>(), StatisticsSortOrder.BestFirst)).Returns(Task.FromResult(new StatisticsResult<PlayerInnings>[] { new StatisticsResult<PlayerInnings>() } as IEnumerable<StatisticsResult<PlayerInnings>>));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<StatisticsSummaryViewModel<Competition>>(((ViewResult)result).Model);
            }
        }
    }
}
