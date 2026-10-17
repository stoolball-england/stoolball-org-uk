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
    /// A page number from the querystring multiplied by the page size can overflow an <c>int</c>. If the offset passed to
    /// SQL Server wraps round to a negative number, SQL Server throws an exception rather than returning an empty page.
    /// </summary>
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class ReadStatisticsForPageBeyondIntRangeTests
    {
        private const int OFFSET_OVERFLOWS_TO_NEGATIVE = 42949674; // 50 * (42949674 - 1) overflows to a negative int
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly Mock<IStatisticsQueryBuilder> _queryBuilder = new();
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();

        public ReadStatisticsForPageBeyondIntRangeTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns(() => (string.Empty, new Dictionary<string, object>()));
        }

        private static StatisticsFilter CreateFilter(int pageNumber)
        {
            return new StatisticsFilter { Paging = new Paging { PageNumber = pageNumber, PageSize = Constants.Defaults.PageSize } };
        }

        [Theory]
        [InlineData(nameof(SqlServerBestPerformanceInAMatchStatisticsDataSource.ReadPlayerInnings), StatisticsSortOrder.BestFirst, OFFSET_OVERFLOWS_TO_NEGATIVE)]
        [InlineData(nameof(SqlServerBestPerformanceInAMatchStatisticsDataSource.ReadPlayerInnings), StatisticsSortOrder.LatestFirst, int.MaxValue)]
        [InlineData(nameof(SqlServerBestPerformanceInAMatchStatisticsDataSource.ReadBowlingFigures), StatisticsSortOrder.BestFirst, OFFSET_OVERFLOWS_TO_NEGATIVE)]
        [InlineData(nameof(SqlServerBestPerformanceInAMatchStatisticsDataSource.ReadBowlingFigures), StatisticsSortOrder.LatestFirst, int.MaxValue)]
        [InlineData(nameof(SqlServerBestPerformanceInAMatchStatisticsDataSource.ReadPlayerIdentityPerformances), null, OFFSET_OVERFLOWS_TO_NEGATIVE)]
        [InlineData(nameof(SqlServerBestPerformanceInAMatchStatisticsDataSource.ReadPlayerIdentityPerformances), null, int.MaxValue)]
        public async Task SqlServerBestPerformanceInAMatchStatisticsDataSource_returns_no_results(string method, StatisticsSortOrder? sortOrder, int pageNumber)
        {
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);
            var filter = CreateFilter(pageNumber);

            var results = method switch
            {
                nameof(dataSource.ReadPlayerInnings) => (await dataSource.ReadPlayerInnings(filter, sortOrder!.Value).ConfigureAwait(false)).Cast<object>(),
                nameof(dataSource.ReadBowlingFigures) => (await dataSource.ReadBowlingFigures(filter, sortOrder!.Value).ConfigureAwait(false)).Cast<object>(),
                nameof(dataSource.ReadPlayerIdentityPerformances) => (await dataSource.ReadPlayerIdentityPerformances(filter).ConfigureAwait(false)).Cast<object>(),
                _ => throw new ArgumentOutOfRangeException(nameof(method))
            };

            Assert.Empty(results);
        }

        [Theory]
        [InlineData(nameof(SqlServerBestPlayerAverageStatisticsDataSource.ReadBestBattingAverage), OFFSET_OVERFLOWS_TO_NEGATIVE)]
        [InlineData(nameof(SqlServerBestPlayerAverageStatisticsDataSource.ReadBestBattingAverage), int.MaxValue)]
        [InlineData(nameof(SqlServerBestPlayerAverageStatisticsDataSource.ReadBestBowlingAverage), OFFSET_OVERFLOWS_TO_NEGATIVE)]
        [InlineData(nameof(SqlServerBestPlayerAverageStatisticsDataSource.ReadBestEconomyRate), OFFSET_OVERFLOWS_TO_NEGATIVE)]
        [InlineData(nameof(SqlServerBestPlayerAverageStatisticsDataSource.ReadBestBattingStrikeRate), OFFSET_OVERFLOWS_TO_NEGATIVE)]
        [InlineData(nameof(SqlServerBestPlayerAverageStatisticsDataSource.ReadBestBowlingStrikeRate), OFFSET_OVERFLOWS_TO_NEGATIVE)]
        public async Task SqlServerBestPlayerAverageStatisticsDataSource_returns_no_results(string method, int pageNumber)
        {
            var dataSource = new SqlServerBestPlayerAverageStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);
            var filter = CreateFilter(pageNumber);

            var results = method switch
            {
                nameof(dataSource.ReadBestBattingAverage) => await dataSource.ReadBestBattingAverage(filter).ConfigureAwait(false),
                nameof(dataSource.ReadBestBowlingAverage) => await dataSource.ReadBestBowlingAverage(filter).ConfigureAwait(false),
                nameof(dataSource.ReadBestEconomyRate) => await dataSource.ReadBestEconomyRate(filter).ConfigureAwait(false),
                nameof(dataSource.ReadBestBattingStrikeRate) => await dataSource.ReadBestBattingStrikeRate(filter).ConfigureAwait(false),
                nameof(dataSource.ReadBestBowlingStrikeRate) => await dataSource.ReadBestBowlingStrikeRate(filter).ConfigureAwait(false),
                _ => throw new ArgumentOutOfRangeException(nameof(method))
            };

            Assert.Empty(results);
        }

        [Theory]
        [InlineData(nameof(SqlServerBestPlayerTotalStatisticsDataSource.ReadMostPlayerInnings), OFFSET_OVERFLOWS_TO_NEGATIVE)]
        [InlineData(nameof(SqlServerBestPlayerTotalStatisticsDataSource.ReadMostPlayerInnings), int.MaxValue)]
        [InlineData(nameof(SqlServerBestPlayerTotalStatisticsDataSource.ReadMostRunsScored), OFFSET_OVERFLOWS_TO_NEGATIVE)]
        [InlineData(nameof(SqlServerBestPlayerTotalStatisticsDataSource.ReadMostRunsScored), int.MaxValue)]
        [InlineData(nameof(SqlServerBestPlayerTotalStatisticsDataSource.ReadMostInningsWithBowling), OFFSET_OVERFLOWS_TO_NEGATIVE)]
        [InlineData(nameof(SqlServerBestPlayerTotalStatisticsDataSource.ReadMostWickets), OFFSET_OVERFLOWS_TO_NEGATIVE)]
        [InlineData(nameof(SqlServerBestPlayerTotalStatisticsDataSource.ReadMostCatches), OFFSET_OVERFLOWS_TO_NEGATIVE)]
        [InlineData(nameof(SqlServerBestPlayerTotalStatisticsDataSource.ReadMostRunOuts), OFFSET_OVERFLOWS_TO_NEGATIVE)]
        [InlineData(nameof(SqlServerBestPlayerTotalStatisticsDataSource.ReadMostPlayerOfTheMatchAwards), OFFSET_OVERFLOWS_TO_NEGATIVE)]
        [InlineData(nameof(SqlServerBestPlayerTotalStatisticsDataSource.ReadMostPlayerOfTheMatchAwards), int.MaxValue)]
        public async Task SqlServerBestPlayerTotalStatisticsDataSource_returns_no_results(string method, int pageNumber)
        {
            var dataSource = new SqlServerBestPlayerTotalStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object, _playerDataSource.Object);
            var filter = CreateFilter(pageNumber);

            var results = method switch
            {
                nameof(dataSource.ReadMostPlayerInnings) => await dataSource.ReadMostPlayerInnings(filter).ConfigureAwait(false),
                nameof(dataSource.ReadMostRunsScored) => await dataSource.ReadMostRunsScored(filter).ConfigureAwait(false),
                nameof(dataSource.ReadMostInningsWithBowling) => await dataSource.ReadMostInningsWithBowling(filter).ConfigureAwait(false),
                nameof(dataSource.ReadMostWickets) => await dataSource.ReadMostWickets(filter).ConfigureAwait(false),
                nameof(dataSource.ReadMostCatches) => await dataSource.ReadMostCatches(filter).ConfigureAwait(false),
                nameof(dataSource.ReadMostRunOuts) => await dataSource.ReadMostRunOuts(filter).ConfigureAwait(false),
                nameof(dataSource.ReadMostPlayerOfTheMatchAwards) => await dataSource.ReadMostPlayerOfTheMatchAwards(filter).ConfigureAwait(false),
                _ => throw new ArgumentOutOfRangeException(nameof(method))
            };

            Assert.Empty(results);
        }

        [Theory]
        [InlineData(OFFSET_OVERFLOWS_TO_NEGATIVE)]
        [InlineData(int.MaxValue)]
        public async Task SqlServerPlayerPerformanceStatisticsDataSource_returns_no_results(int pageNumber)
        {
            var dataSource = new SqlServerPlayerPerformanceStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(CreateFilter(pageNumber)).ConfigureAwait(false);

            Assert.Empty(results);
        }
    }
}
