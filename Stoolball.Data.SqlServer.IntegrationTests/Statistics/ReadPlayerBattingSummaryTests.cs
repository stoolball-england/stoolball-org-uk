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
    public class ReadPlayerBattingSummaryTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly DateRangeGenerator _dateRangeGenerator = new DateRangeGenerator();

        public ReadPlayerBattingSummaryTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_batting_statistics_throws_ArgumentNullException_with_no_filter()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await dataSource.ReadBattingStatistics(null).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_throws_ArgumentException_with_no_player()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            await Assert.ThrowsAsync<ArgumentException>(async () => await dataSource.ReadBattingStatistics(new StatisticsFilter()).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_throws_ArgumentException_with_no_player_id()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            await Assert.ThrowsAsync<ArgumentException>(async () => await dataSource.ReadBattingStatistics(new StatisticsFilter { Player = new Player() }).ConfigureAwait(false)).ConfigureAwait(false);
        }

        private async Task TestTotalInnings(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {
            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);
                var result = await dataSource.ReadBattingStatistics(filter).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Equal(matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Count(x => x.Batter.Player.PlayerId == player.PlayerId && x.DismissalType != DismissalType.DidNotBat), result.TotalInnings);
            }
        }

        private async Task TestTotalInningsWithRunsScored(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {
            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);
                var result = await dataSource.ReadBattingStatistics(filter).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Equal(matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Count(x => x.Batter.Player.PlayerId == player.PlayerId && x.RunsScored.HasValue), result.TotalInningsWithRunsScored);
            }
        }

        private async Task TestTotalInningsWithRunsScoredAndBallsFaced(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {
            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);
                var result = await dataSource.ReadBattingStatistics(filter).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Equal(matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Count(x => x.Batter.Player.PlayerId == player.PlayerId && x.RunsScored.HasValue && x.BallsFaced.HasValue), result.TotalInningsWithRunsScoredAndBallsFaced);
            }
        }

        private async Task TestNotOuts(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {
            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);
                var result = await dataSource.ReadBattingStatistics(filter).ConfigureAwait(false);

                Assert.NotNull(result);
                var expectedNotOuts = matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Count(x => x.Batter.Player.PlayerId == player.PlayerId &&
                    (x.DismissalType == DismissalType.NotOut || x.DismissalType == DismissalType.Retired || x.DismissalType == DismissalType.RetiredHurt));
                Assert.Equal(expectedNotOuts, result.NotOuts);
            }
        }


        private async Task TestTotalRunsScored(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {
            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);
                var result = await dataSource.ReadBattingStatistics(filter).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Equal(matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Where(x => x.Batter.Player.PlayerId == player.PlayerId && x.RunsScored.HasValue).Sum(x => x.RunsScored), result.TotalRunsScored);
            }
        }

        private async Task TestFifties(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {
            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);
                var result = await dataSource.ReadBattingStatistics(filter).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Equal(matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Count(x => x.Batter.Player.PlayerId == player.PlayerId && x.RunsScored >= 50), result.Fifties);
            }
        }

        private async Task TestHundreds(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {
            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);
                var result = await dataSource.ReadBattingStatistics(filter).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Equal(matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Count(x => x.Batter.Player.PlayerId == player.PlayerId && x.RunsScored >= 100), result.Hundreds);
            }
        }

        private async Task TestBestInnings(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {
            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);
                var result = await dataSource.ReadBattingStatistics(filter).ConfigureAwait(false);

                Assert.NotNull(result);

                var bestRunsScored = matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Where(x => x.Batter.Player.PlayerId == player.PlayerId).Max(x => x.RunsScored);
                if (bestRunsScored.HasValue)
                {
                    Assert.Equal(bestRunsScored, result.BestInningsRunsScored);
                    Assert.Equal(matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Where(x => x.Batter.Player.PlayerId == player.PlayerId && x.RunsScored == bestRunsScored)
                        .Any(x => x.DismissalType == DismissalType.NotOut || x.DismissalType == DismissalType.Retired || x.DismissalType == DismissalType.RetiredHurt), !result.BestInningsWasDismissed);
                }
                else
                {
                    Assert.Null(result.BestInningsRunsScored);
                }
            }
        }

        private async Task TestStrikeRate(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {
            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);
                var result = await dataSource.ReadBattingStatistics(filter).ConfigureAwait(false);

                Assert.NotNull(result);

                var totalRunsScored = matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Where(x => x.Batter.Player.PlayerId == player.PlayerId && x.RunsScored.HasValue && x.BallsFaced.HasValue).Sum(x => x.RunsScored);
                var totalBallsFaced = matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Where(x => x.Batter.Player.PlayerId == player.PlayerId && x.RunsScored.HasValue && x.BallsFaced.HasValue).Sum(x => x.BallsFaced);
                if (totalBallsFaced > 0)
                {
                    var strikeRate = (((decimal)totalRunsScored) / totalBallsFaced.Value) * 100;
                    Assert.Equal(Math.Round(strikeRate, 2), Math.Round(result.StrikeRate.Value, 2));
                }
                else
                {
                    Assert.Null(result.StrikeRate);
                }
            }
        }

        private async Task TestAverage(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, IEnumerable<Stoolball.Matches.Match> matches)
        {
            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);
                var result = await dataSource.ReadBattingStatistics(filter).ConfigureAwait(false);

                Assert.NotNull(result);

                var totalRunsScored = matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Where(x => x.Batter.Player.PlayerId == player.PlayerId && x.RunsScored.HasValue).Sum(x => x.RunsScored);
                var totalOuts = matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Count(x => x.Batter.Player.PlayerId == player.PlayerId && x.RunsScored.HasValue && x.DismissalType != DismissalType.NotOut && x.DismissalType != DismissalType.Retired && x.DismissalType != DismissalType.RetiredHurt);
                if (totalOuts > 0)
                {
                    var average = ((decimal)totalRunsScored) / totalOuts;
                    Assert.Equal(Math.Round(average, 2), Math.Round(result.Average.Value, 2));
                }
                else
                {
                    Assert.Null(result.Average);
                }
            }
        }

        [Fact]
        public async Task Read_batting_statistics_returns_TotalInnings_supporting_unfiltered_matches()
        {
            await TestTotalInnings(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_returns_TotalInningsWithRunsScored_supporting_unfiltered_matches()
        {
            await TestTotalInningsWithRunsScored(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_returns_TotalInningsWithRunsScoredAndBallsFaced_supporting_unfiltered_matches()
        {
            await TestTotalInningsWithRunsScoredAndBallsFaced(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_returns_NotOuts_supporting_unfiltered_matches()
        {
            await TestNotOuts(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_returns_TotalRunsScored_supporting_unfiltered_matches()
        {
            await TestTotalRunsScored(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_returns_Fifties_supporting_unfiltered_matches()
        {
            await TestFifties(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_returns_Hundreds_supporting_unfiltered_matches()
        {
            await TestHundreds(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_returns_BestInnings_supporting_unfiltered_matches()
        {
            await TestBestInnings(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_returns_StrikeRate_supporting_unfiltered_matches()
        {
            await TestStrikeRate(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_returns_Average_supporting_unfiltered_matches()
        {
            await TestAverage(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                _databaseFixture.TestData.Matches).ConfigureAwait(false);
        }


        [Fact]
        public async Task Read_batting_statistics_returns_TotalInnings_supporting_filter_by_date()
        {
            var filter = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestTotalInnings(filter,
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_returns_TotalInningsWithRunsScored_supporting_filter_by_date()
        {
            var filter = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestTotalInningsWithRunsScored(filter,
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_returns_TotalInningsWithRunsScoredAndBallsFaced_supporting_filter_by_date()
        {
            var filter = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestTotalInningsWithRunsScoredAndBallsFaced(filter,
              " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
              new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } },
              _databaseFixture.TestData.Matches.Where(x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_returns_NotOuts_supporting_filter_by_date()
        {
            var filter = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestNotOuts(filter,
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_returns_TotalRunsScored_supporting_filter_by_date()
        {
            var filter = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestTotalRunsScored(filter,
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_returns_Fifties_supporting_filter_by_date()
        {
            var filter = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestFifties(filter,
              " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
              new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } },
              _databaseFixture.TestData.Matches.Where(x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_returns_Hundreds_supporting_filter_by_date()
        {
            var filter = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestHundreds(filter,
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_returns_BestInnings_supporting_filter_by_date()
        {
            var filter = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestBestInnings(filter,
              " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
              new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } },
              _databaseFixture.TestData.Matches.Where(x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_returns_StrikeRate_supporting_filter_by_date()
        {
            var filter = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestStrikeRate(filter,
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } },
               _databaseFixture.TestData.Matches.Where(x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_returns_Average_supporting_filter_by_date()
        {
            var filter = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestAverage(filter,
                " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
                new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } },
                _databaseFixture.TestData.Matches.Where(x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate)).ConfigureAwait(false);
        }
    }
}
