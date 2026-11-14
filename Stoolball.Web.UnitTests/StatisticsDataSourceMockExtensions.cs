using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Statistics;

namespace Stoolball.Web.UnitTests
{
    /// <summary>
    /// Moq returns an empty <see cref="System.Collections.Generic.IEnumerable{T}"/> from a method that hasn't been set up,
    /// but returns <c>null</c> for <see cref="StatisticsResults{T}"/>. These set up empty results for every ranked statistic
    /// instead. Call them when creating the mock, so that setups in a test take precedence.
    /// </summary>
    internal static class StatisticsDataSourceMockExtensions
    {
        internal static Mock<IBestPerformanceInAMatchStatisticsDataSource> WithEmptyResults(this Mock<IBestPerformanceInAMatchStatisticsDataSource> mock)
        {
            mock.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>(), It.IsAny<StatisticsSortOrder>())).ReturnsAsync(new StatisticsResults<PlayerInnings>([]));
            mock.Setup(x => x.ReadBowlingFigures(It.IsAny<StatisticsFilter>(), It.IsAny<StatisticsSortOrder>())).ReturnsAsync(new StatisticsResults<BowlingFigures>([]));
            return mock;
        }

        internal static Mock<IBestPlayerTotalStatisticsDataSource> WithEmptyResults(this Mock<IBestPlayerTotalStatisticsDataSource> mock)
        {
            mock.Setup(x => x.ReadMostPlayerInnings(It.IsAny<StatisticsFilter>())).ReturnsAsync(new StatisticsResults<BestStatistic>([]));
            mock.Setup(x => x.ReadMostRunsScored(It.IsAny<StatisticsFilter>())).ReturnsAsync(new StatisticsResults<BestStatistic>([]));
            mock.Setup(x => x.ReadMostWickets(It.IsAny<StatisticsFilter>())).ReturnsAsync(new StatisticsResults<BestStatistic>([]));
            mock.Setup(x => x.ReadMostInningsWithBowling(It.IsAny<StatisticsFilter>())).ReturnsAsync(new StatisticsResults<BestStatistic>([]));
            mock.Setup(x => x.ReadMostCatches(It.IsAny<StatisticsFilter>())).ReturnsAsync(new StatisticsResults<BestStatistic>([]));
            mock.Setup(x => x.ReadMostRunOuts(It.IsAny<StatisticsFilter>())).ReturnsAsync(new StatisticsResults<BestStatistic>([]));
            mock.Setup(x => x.ReadMostPlayerOfTheMatchAwards(It.IsAny<StatisticsFilter>())).ReturnsAsync(new StatisticsResults<BestStatistic>([]));
            return mock;
        }
    }
}
