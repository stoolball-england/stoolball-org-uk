using System;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Matches;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.StatisticsDataSourceIntegrationTestCollection)]
    public class SqlServerPlayerSummaryStatisticsDataSourceTests
    {
        private readonly SqlServerStatisticsDataSourceFixture _databaseFixture;

        public SqlServerPlayerSummaryStatisticsDataSourceTests(SqlServerStatisticsDataSourceFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_batting_statistics_throws_ArgumentNullException_with_no_filter()
        {
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await dataSource.ReadBattingStatistics(null).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_throws_ArgumentException_with_no_player()
        {
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory);

            await Assert.ThrowsAsync<ArgumentException>(async () => await dataSource.ReadBattingStatistics(new StatisticsFilter()).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_throws_ArgumentException_with_no_player_id()
        {
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory);

            await Assert.ThrowsAsync<ArgumentException>(async () => await dataSource.ReadBattingStatistics(new StatisticsFilter { Player = new Player() }).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_returns_TotalInnings()
        {
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory);

            foreach (var player in _databaseFixture.Players)
            {
                var result = await dataSource.ReadBattingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Equal(_databaseFixture.PlayerInnings.Count(x => x.Batter.Player.PlayerId == player.PlayerId && x.DismissalType != DismissalType.DidNotBat), result.TotalInnings);
            }
        }

        [Fact]
        public async Task Read_batting_statistics_returns_TotalInningsWithRunsScored()
        {
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory);

            foreach (var player in _databaseFixture.Players)
            {
                var result = await dataSource.ReadBattingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Equal(_databaseFixture.PlayerInnings.Count(x => x.Batter.Player.PlayerId == player.PlayerId && x.RunsScored.HasValue), result.TotalInningsWithRunsScored);
            }
        }

        [Fact]
        public async Task Read_batting_statistics_returns_TotalInningsWithRunsScoredAndBallsFaced()
        {
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory);

            foreach (var player in _databaseFixture.Players)
            {
                var result = await dataSource.ReadBattingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Equal(_databaseFixture.PlayerInnings.Count(x => x.Batter.Player.PlayerId == player.PlayerId && x.RunsScored.HasValue && x.BallsFaced.HasValue), result.TotalInningsWithRunsScoredAndBallsFaced);
            }
        }

        [Fact]
        public async Task Read_batting_statistics_returns_NotOuts()
        {
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory);

            foreach (var player in _databaseFixture.Players)
            {
                var result = await dataSource.ReadBattingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Equal(_databaseFixture.PlayerInnings.Count(x => x.Batter.Player.PlayerId == player.PlayerId &&
                    (x.DismissalType == DismissalType.NotOut || x.DismissalType == DismissalType.Retired || x.DismissalType == DismissalType.RetiredHurt)), result.NotOuts);
            }
        }

        [Fact]
        public async Task Read_batting_statistics_returns_TotalRunsScored()
        {
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory);

            foreach (var player in _databaseFixture.Players)
            {
                var result = await dataSource.ReadBattingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Equal(_databaseFixture.PlayerInnings.Where(x => x.Batter.Player.PlayerId == player.PlayerId && x.RunsScored.HasValue).Sum(x => x.RunsScored), result.TotalRunsScored);
            }
        }

        [Fact]
        public async Task Read_batting_statistics_returns_Fifties()
        {
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory);

            foreach (var player in _databaseFixture.Players)
            {
                var result = await dataSource.ReadBattingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Equal(_databaseFixture.PlayerInnings.Count(x => x.Batter.Player.PlayerId == player.PlayerId && x.RunsScored >= 50), result.Fifties);
            }
        }

        [Fact]
        public async Task Read_batting_statistics_returns_Hundreds()
        {
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory);

            foreach (var player in _databaseFixture.Players)
            {
                var result = await dataSource.ReadBattingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Equal(_databaseFixture.PlayerInnings.Count(x => x.Batter.Player.PlayerId == player.PlayerId && x.RunsScored >= 100), result.Hundreds);
            }
        }

        [Fact]
        public async Task Read_batting_statistics_returns_BestInnings()
        {
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory);

            foreach (var player in _databaseFixture.Players)
            {
                var result = await dataSource.ReadBattingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                Assert.NotNull(result);

                var bestRunsScored = _databaseFixture.PlayerInnings.Where(x => x.Batter.Player.PlayerId == player.PlayerId).Max(x => x.RunsScored);
                Assert.Equal(bestRunsScored, result.BestInningsRunsScored);
                Assert.Equal(_databaseFixture.PlayerInnings.Where(x => x.Batter.Player.PlayerId == player.PlayerId && x.RunsScored == bestRunsScored)
                    .Any(x => x.DismissalType == DismissalType.NotOut || x.DismissalType == DismissalType.Retired || x.DismissalType == DismissalType.RetiredHurt), !result.BestInningsWasDismissed);
            }
        }

        [Fact]
        public async Task Read_batting_statistics_returns_StrikeRate()
        {
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory);

            foreach (var player in _databaseFixture.Players)
            {
                var result = await dataSource.ReadBattingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                Assert.NotNull(result);

                var totalRunsScored = _databaseFixture.PlayerInnings.Where(x => x.Batter.Player.PlayerId == player.PlayerId && x.RunsScored.HasValue && x.BallsFaced.HasValue).Sum(x => x.RunsScored);
                var totalBallsFaced = _databaseFixture.PlayerInnings.Where(x => x.Batter.Player.PlayerId == player.PlayerId && x.RunsScored.HasValue && x.BallsFaced.HasValue).Sum(x => x.BallsFaced);
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

        [Fact]
        public async Task Read_batting_statistics_returns_Average()
        {
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory);

            foreach (var player in _databaseFixture.Players)
            {
                var result = await dataSource.ReadBattingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                Assert.NotNull(result);

                var totalRunsScored = _databaseFixture.PlayerInnings.Where(x => x.Batter.Player.PlayerId == player.PlayerId && x.RunsScored.HasValue).Sum(x => x.RunsScored);
                var totalOuts = _databaseFixture.PlayerInnings.Count(x => x.Batter.Player.PlayerId == player.PlayerId && x.RunsScored.HasValue && x.DismissalType != DismissalType.NotOut && x.DismissalType != DismissalType.Retired && x.DismissalType != DismissalType.RetiredHurt);
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
    }
}
