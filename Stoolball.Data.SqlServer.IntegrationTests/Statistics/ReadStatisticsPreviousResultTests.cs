using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Navigation;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    /// <summary>
    /// Ranked statistics return the result before the page, so that the first result on a page can be shown as equal to the
    /// last result on the page before.
    /// </summary>
    /// <remarks>
    /// Results are compared using every field in the query's ORDER BY clause, because results which are equal on all of those
    /// fields can be returned in any order, so a different query could return a different one of them at the same position.
    /// </remarks>
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class ReadStatisticsPreviousResultTests
    {
        private const int PAGE_SIZE = 3;
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly Mock<IStatisticsQueryBuilder> _queryBuilder = new();
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();
        private readonly SqlServerBestPerformanceInAMatchStatisticsDataSource _bestPerformanceDataSource;
        private readonly SqlServerBestPlayerTotalStatisticsDataSource _totalDataSource;
        private readonly SqlServerBestPlayerAverageStatisticsDataSource _averageDataSource;

        public ReadStatisticsPreviousResultTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns(() => (string.Empty, new Dictionary<string, object>()));
            _playerDataSource.Setup(x => x.ReadPlayers(It.IsAny<PlayerFilter>())).Returns(Task.FromResult(_databaseFixture.TestData.Players));

            _bestPerformanceDataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);
            _totalDataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);
            _averageDataSource = new SqlServerBestPlayerAverageStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);
        }

        public static TheoryData<string> RankedStatistics => new()
        {
            nameof(SqlServerBestPerformanceInAMatchStatisticsDataSource.ReadPlayerInnings),
            nameof(SqlServerBestPerformanceInAMatchStatisticsDataSource.ReadBowlingFigures),
            nameof(SqlServerBestPlayerTotalStatisticsDataSource.ReadMostPlayerInnings),
            nameof(SqlServerBestPlayerTotalStatisticsDataSource.ReadMostRunsScored),
            nameof(SqlServerBestPlayerTotalStatisticsDataSource.ReadMostInningsWithBowling),
            nameof(SqlServerBestPlayerTotalStatisticsDataSource.ReadMostWickets),
            nameof(SqlServerBestPlayerTotalStatisticsDataSource.ReadMostCatches),
            nameof(SqlServerBestPlayerTotalStatisticsDataSource.ReadMostRunOuts),
            nameof(SqlServerBestPlayerTotalStatisticsDataSource.ReadMostPlayerOfTheMatchAwards),
            nameof(SqlServerBestPlayerAverageStatisticsDataSource.ReadBestBattingAverage),
            nameof(SqlServerBestPlayerAverageStatisticsDataSource.ReadBestBowlingAverage),
            nameof(SqlServerBestPlayerAverageStatisticsDataSource.ReadBestEconomyRate),
            nameof(SqlServerBestPlayerAverageStatisticsDataSource.ReadBestBattingStrikeRate),
            nameof(SqlServerBestPlayerAverageStatisticsDataSource.ReadBestBowlingStrikeRate),
        };

        public static TheoryData<string> RankedStatisticsSupportingMaxResults => new()
        {
            nameof(SqlServerBestPerformanceInAMatchStatisticsDataSource.ReadPlayerInnings),
            nameof(SqlServerBestPerformanceInAMatchStatisticsDataSource.ReadBowlingFigures),
            nameof(SqlServerBestPlayerTotalStatisticsDataSource.ReadMostRunsScored),
            nameof(SqlServerBestPlayerTotalStatisticsDataSource.ReadMostWickets),
            nameof(SqlServerBestPlayerTotalStatisticsDataSource.ReadMostCatches),
            nameof(SqlServerBestPlayerTotalStatisticsDataSource.ReadMostRunOuts),
        };

        private static StatisticsFilter CreateFilter(int pageNumber, int pageSize = PAGE_SIZE) => new() { Paging = new Paging { PageNumber = pageNumber, PageSize = pageSize } };

        [Theory]
        [MemberData(nameof(RankedStatistics))]
        public async Task First_page_has_no_previous_result(string method)
        {
            var (results, previousResult) = await ReadRankingKeys(method, CreateFilter(1)).ConfigureAwait(false);

            Assert.Equal(PAGE_SIZE, results.Count);
            Assert.Null(previousResult);
        }

        [Theory]
        [MemberData(nameof(RankedStatistics))]
        public async Task Later_page_returns_last_result_of_page_before_as_previous_result(string method)
        {
            var (firstPage, _) = await ReadRankingKeys(method, CreateFilter(1)).ConfigureAwait(false);
            var (secondPage, previousResult) = await ReadRankingKeys(method, CreateFilter(2)).ConfigureAwait(false);
            var (bothPages, _) = await ReadRankingKeys(method, CreateFilter(1, PAGE_SIZE * 2)).ConfigureAwait(false);

            Assert.Equal(PAGE_SIZE, firstPage.Count);
            Assert.Equal(firstPage[^1], previousResult);
            Assert.Equal(bothPages.Skip(PAGE_SIZE), secondPage);
        }

        [Theory]
        [MemberData(nameof(RankedStatistics))]
        public async Task Page_after_last_result_returns_last_result_as_previous_result(string method)
        {
            var (allResults, _) = await ReadRankingKeys(method, CreateFilter(1, int.MaxValue)).ConfigureAwait(false);
            Assert.NotEmpty(allResults);

            var (results, previousResult) = await ReadRankingKeys(method, CreateFilter(2, allResults.Count)).ConfigureAwait(false);

            Assert.Empty(results);
            Assert.Equal(allResults[^1], previousResult);
        }

        [Theory]
        [MemberData(nameof(RankedStatisticsSupportingMaxResults))]
        public async Task Max_results_filter_has_no_previous_result(string method)
        {
            var filter = CreateFilter(2);
            filter.MaxResultsAllowingExtraResultsIfValuesAreEqual = PAGE_SIZE;

            var (results, previousResult) = await ReadRankingKeys(method, filter).ConfigureAwait(false);

            Assert.True(results.Count >= PAGE_SIZE);
            Assert.Null(previousResult);
        }

        /// <summary>
        /// Reads a statistic, and gets the fields it is sorted by for each result on the page and for the previous result.
        /// </summary>
        private async Task<(List<string> Results, string? PreviousResult)> ReadRankingKeys(string method, StatisticsFilter filter)
        {
            return method switch
            {
                nameof(_bestPerformanceDataSource.ReadPlayerInnings) => RankingKeys(await _bestPerformanceDataSource.ReadPlayerInnings(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false), x => $"{x.RunsScored} {x.DismissalType}"),
                nameof(_bestPerformanceDataSource.ReadBowlingFigures) => RankingKeys(await _bestPerformanceDataSource.ReadBowlingFigures(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false), x => $"{x.Wickets} {x.RunsConceded}"),
                nameof(_totalDataSource.ReadMostPlayerInnings) => RankingKeys(await _totalDataSource.ReadMostPlayerInnings(filter).ConfigureAwait(false), x => $"{x.Total}"),
                nameof(_totalDataSource.ReadMostInningsWithBowling) => RankingKeys(await _totalDataSource.ReadMostInningsWithBowling(filter).ConfigureAwait(false), x => $"{x.Total}"),
                nameof(_totalDataSource.ReadMostRunsScored) => RankingKeys(await _totalDataSource.ReadMostRunsScored(filter).ConfigureAwait(false), TotalRankingKey),
                nameof(_totalDataSource.ReadMostWickets) => RankingKeys(await _totalDataSource.ReadMostWickets(filter).ConfigureAwait(false), TotalRankingKey),
                nameof(_totalDataSource.ReadMostCatches) => RankingKeys(await _totalDataSource.ReadMostCatches(filter).ConfigureAwait(false), TotalRankingKey),
                nameof(_totalDataSource.ReadMostRunOuts) => RankingKeys(await _totalDataSource.ReadMostRunOuts(filter).ConfigureAwait(false), TotalRankingKey),
                nameof(_totalDataSource.ReadMostPlayerOfTheMatchAwards) => RankingKeys(await _totalDataSource.ReadMostPlayerOfTheMatchAwards(filter).ConfigureAwait(false), x => $"{x.Total} {x.TotalMatches}"),
                nameof(_averageDataSource.ReadBestBattingAverage) => RankingKeys(await _averageDataSource.ReadBestBattingAverage(filter).ConfigureAwait(false), AverageRankingKey),
                nameof(_averageDataSource.ReadBestBowlingAverage) => RankingKeys(await _averageDataSource.ReadBestBowlingAverage(filter).ConfigureAwait(false), AverageRankingKey),
                nameof(_averageDataSource.ReadBestEconomyRate) => RankingKeys(await _averageDataSource.ReadBestEconomyRate(filter).ConfigureAwait(false), AverageRankingKey),
                nameof(_averageDataSource.ReadBestBattingStrikeRate) => RankingKeys(await _averageDataSource.ReadBestBattingStrikeRate(filter).ConfigureAwait(false), AverageRankingKey),
                nameof(_averageDataSource.ReadBestBowlingStrikeRate) => RankingKeys(await _averageDataSource.ReadBestBowlingStrikeRate(filter).ConfigureAwait(false), AverageRankingKey),
                _ => throw new ArgumentOutOfRangeException(nameof(method))
            };
        }

        private static string TotalRankingKey(BestStatistic result) => $"{result.Total} {result.TotalInnings} {result.TotalMatches}";

        private static string AverageRankingKey(BestStatistic result) => $"{result.Average} {result.TotalInnings} {result.TotalMatches}";

        private static (List<string> Results, string? PreviousResult) RankingKeys<T>(StatisticsResults<T> results, Func<T, string> rankingKey)
        {
            return (results.Select(x => rankingKey(x.Result!)).ToList(),
                    results.PreviousResult is null ? null : rankingKey(results.PreviousResult.Result!));
        }
    }
}
