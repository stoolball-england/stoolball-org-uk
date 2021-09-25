using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Testing;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class ReadPlayerFieldingSummaryTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly DateRangeGenerator _dateRangeGenerator = new DateRangeGenerator();

        public ReadPlayerFieldingSummaryTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_fielding_statistics_throws_ArgumentNullException_with_no_filter()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await dataSource.ReadFieldingStatistics(null).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_fielding_statistics_throws_ArgumentException_with_no_player()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            await Assert.ThrowsAsync<ArgumentException>(async () => await dataSource.ReadFieldingStatistics(new StatisticsFilter()).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_fielding_statistics_throws_ArgumentException_with_no_player_id()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            await Assert.ThrowsAsync<ArgumentException>(async () => await dataSource.ReadFieldingStatistics(new StatisticsFilter { Player = new Player() }).ConfigureAwait(false)).ConfigureAwait(false);
        }

        private async Task TestTotalCatches(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {

            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

                var result = await dataSource.ReadFieldingStatistics(filter).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Equal(matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings)
                    .Count(x => (x.DismissalType == DismissalType.Caught && x.DismissedBy != null && x.DismissedBy.Player.PlayerId == player.PlayerId) ||
                                (x.DismissalType == DismissalType.CaughtAndBowled && x.Bowler != null && x.Bowler.Player.PlayerId == player.PlayerId)), result.TotalCatches);
            }
        }

        private async Task TestTotalRunOuts(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {

            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

                var result = await dataSource.ReadFieldingStatistics(filter).ConfigureAwait(false);

                Assert.Equal(matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings)
                    .Count(x => x.DismissalType == DismissalType.RunOut && x.DismissedBy != null && x.DismissedBy.Player.PlayerId == player.PlayerId), result.TotalRunOuts);
            }
        }

        private async Task TestMostCatches(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {

            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

                var result = await dataSource.ReadFieldingStatistics(filter).ConfigureAwait(false);

                var best = matches.SelectMany(x => x.MatchInnings)
                                    .Where(i => i.PlayerInnings.Any(x => (x.DismissalType == DismissalType.Caught && x.DismissedBy != null && x.DismissedBy.Player.PlayerId == player.PlayerId) ||
                                                                            (x.DismissalType == DismissalType.CaughtAndBowled && x.Bowler != null && x.Bowler.Player.PlayerId == player.PlayerId)))
                                    .Select(i => i.PlayerInnings.Count(x => (x.DismissalType == DismissalType.Caught && x.DismissedBy != null && x.DismissedBy.Player.PlayerId == player.PlayerId) ||
                                                                            (x.DismissalType == DismissalType.CaughtAndBowled && x.Bowler != null && x.Bowler.Player.PlayerId == player.PlayerId)))
                                    .OrderByDescending(x => x)
                                    .FirstOrDefault();

                Assert.Equal(best, result.MostCatches);
            }
        }

        private async Task TestMostRunOuts(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {

            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

                var result = await dataSource.ReadFieldingStatistics(filter).ConfigureAwait(false);

                var best = matches.SelectMany(x => x.MatchInnings)
                                    .Where(i => i.PlayerInnings.Any(x => x.DismissalType == DismissalType.RunOut && x.DismissedBy != null && x.DismissedBy.Player.PlayerId == player.PlayerId))
                                    .Select(i => i.PlayerInnings.Count(x => x.DismissalType == DismissalType.RunOut && x.DismissedBy != null && x.DismissedBy.Player.PlayerId == player.PlayerId))
                                    .OrderByDescending(x => x)
                                    .FirstOrDefault();

                Assert.Equal(best, result.MostRunOuts);
            }
        }

        [Fact]
        public async Task Read_fielding_statistics_returns_TotalCatches_supporting_unfiltered_matches()
        {
            await TestTotalCatches(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_fielding_statistics_returns_TotalRunOuts_supporting_unfiltered_matches()
        {
            await TestTotalRunOuts(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_fielding_statistics_returns_MostCatches_supporting_unfiltered_matches()
        {
            await TestMostCatches(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_fielding_statistics_returns_MostRunOuts_supporting_unfiltered_matches()
        {
            await TestMostRunOuts(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_fielding_statistics_returns_TotalCatches_supporting_filter_by_date()
        {
            var (fromDate, untilDate) = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestTotalCatches(new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate },
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", fromDate }, { "UntilDate", untilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= fromDate && x.StartTime <= untilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_fielding_statistics_returns_TotalRunOuts_supporting_filter_by_date()
        {
            var (fromDate, untilDate) = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestTotalRunOuts(new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate },
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", fromDate }, { "UntilDate", untilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= fromDate && x.StartTime <= untilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_fielding_statistics_returns_MostCatches_supporting_filter_by_date()
        {
            var (fromDate, untilDate) = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestMostCatches(new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate },
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", fromDate }, { "UntilDate", untilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= fromDate && x.StartTime <= untilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_fielding_statistics_returns_MostRunOuts_supporting_filter_by_date()
        {
            var (fromDate, untilDate) = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestMostRunOuts(new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate },
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", fromDate }, { "UntilDate", untilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= fromDate && x.StartTime <= untilDate)).ConfigureAwait(false);
        }
    }
}
