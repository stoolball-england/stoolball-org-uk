using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Matches;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class ReadPlayerBowlingSummaryTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;

        public ReadPlayerBowlingSummaryTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_bowling_statistics_throws_ArgumentNullException_with_no_filter()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await dataSource.ReadBowlingStatistics(null).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_throws_ArgumentException_with_no_player()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            await Assert.ThrowsAsync<ArgumentException>(async () => await dataSource.ReadBowlingStatistics(new StatisticsFilter()).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_throws_ArgumentException_with_no_player_id()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            await Assert.ThrowsAsync<ArgumentException>(async () => await dataSource.ReadBowlingStatistics(new StatisticsFilter { Player = new Player() }).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_TotalInnings()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            foreach (var player in _databaseFixture.TestData.Players)
            {
                var result = await dataSource.ReadBowlingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Equal(_databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings).Count(x =>
                        x.OversBowled.Any(o => o.Bowler.Player.PlayerId == player.PlayerId) ||
                        x.PlayerInnings.Any(pi => pi.Bowler?.Player.PlayerId == player.PlayerId)
                    ), result.TotalInnings);
            }
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_TotalInningsWithRunsConceded()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            foreach (var player in _databaseFixture.TestData.Players)
            {
                var result = await dataSource.ReadBowlingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Equal(_databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings).Count(x =>
                        x.OversBowled.Any(o => o.Bowler.Player.PlayerId == player.PlayerId && o.RunsConceded.HasValue)),
                        result.TotalInningsWithRunsConceded);
            }
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_TotalInningsWithBallsBowled()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            foreach (var player in _databaseFixture.TestData.Players)
            {
                var result = await dataSource.ReadBowlingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Equal(_databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings).Count(x =>
                        x.BowlingFigures.Any(o => o.Bowler.Player.PlayerId == player.PlayerId && o.Overs.HasValue)),
                        result.TotalInningsWithBallsBowled);
            }
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_TotalOvers()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);
            var oversHelper = new OversHelper();

            foreach (var player in _databaseFixture.TestData.Players)
            {
                var result = await dataSource.ReadBowlingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                var ballsBowled = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings)
                        .SelectMany(x => x.BowlingFigures.Where(o => o.Bowler.Player.PlayerId == player.PlayerId && o.Overs.HasValue))
                        .Sum(o => oversHelper.OversToBallsBowled(o.Overs.Value));

                Assert.NotNull(result);
                Assert.Equal(oversHelper.BallsBowledToOvers(ballsBowled), result.TotalOvers);
            }
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_TotalMaidens()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            foreach (var player in _databaseFixture.TestData.Players)
            {
                var result = await dataSource.ReadBowlingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                var maidens = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings)
                        .SelectMany(x => x.BowlingFigures.Where(o => o.Bowler.Player.PlayerId == player.PlayerId && o.Maidens.HasValue))
                        .Sum(x => x.Maidens);

                Assert.NotNull(result);
                Assert.Equal(maidens, result.TotalMaidens);
            }
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_TotalRunsConceded()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            foreach (var player in _databaseFixture.TestData.Players)
            {
                var result = await dataSource.ReadBowlingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                var runsConceded = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings)
                       .SelectMany(x => x.BowlingFigures.Where(o => o.Bowler.Player.PlayerId == player.PlayerId && o.RunsConceded.HasValue))
                       .Sum(x => x.RunsConceded);

                Assert.NotNull(result);
                Assert.Equal(runsConceded, result.TotalRunsConceded);
            }
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_TotalWickets()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            foreach (var player in _databaseFixture.TestData.Players)
            {
                var result = await dataSource.ReadBowlingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                var wickets = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings)
                       .SelectMany(x => x.BowlingFigures.Where(o => o.Bowler.Player.PlayerId == player.PlayerId))
                       .Sum(x => x.Wickets);

                Assert.NotNull(result);
                Assert.Equal(wickets, result.TotalWickets);
            }
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_FiveWicketInnings()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var totalFiveWicketHaulsFound = 0;
            foreach (var player in _databaseFixture.TestData.Players)
            {
                var result = await dataSource.ReadBowlingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                var wicketHauls = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings)
                        .SelectMany(x => x.BowlingFigures.Where(o => o.Bowler.Player.PlayerId == player.PlayerId))
                        .Count(x => x.Wickets >= 5);
                if (wicketHauls > 0) { totalFiveWicketHaulsFound++; }

                Assert.NotNull(result);
                Assert.Equal(wicketHauls, result.FiveWicketInnings);
            }

            Assert.True(totalFiveWicketHaulsFound > 0);
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_BestBowling()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            foreach (var player in _databaseFixture.TestData.Players)
            {
                var result = await dataSource.ReadBowlingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                var best = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings) // get all innings...
                .SelectMany(x => x.BowlingFigures.Where(o => o.Bowler.Player.PlayerId == player.PlayerId)) // ...where this player bowled, and select their bowling figures
                .GroupBy(x => x.MatchInnings.MatchInningsId, x => x, (matchInningsId, bowlingFigures) => new BowlingFigures // combine any multiple identities for the player into new bowling figures
                {
                    Wickets = bowlingFigures.Sum(bf => bf.Wickets),
                    RunsConceded = bowlingFigures.Any(bf => bf.RunsConceded.HasValue) ? bowlingFigures.Sum(bf => bf.RunsConceded) : null
                })
                .OrderByDescending(x => x.Wickets).ThenByDescending(x => x.RunsConceded.HasValue ? 1 : 0).ThenBy(x => x.RunsConceded) // then sort them by best bowling first
                .FirstOrDefault(); // and select the top one, if there is one

                Assert.NotNull(result);
                if (best != null)
                {
                    Assert.NotNull(result.BestInningsWickets);
                    Assert.Equal(best.Wickets, result.BestInningsWickets.Value);

                    if (best.RunsConceded.HasValue)
                    {
                        Assert.Equal(best.RunsConceded, result.BestInningsRunsConceded.Value);
                    }
                    else
                    {
                        Assert.Null(result.BestInningsRunsConceded);
                    }
                }
                else
                {
                    Assert.Null(result.BestInningsWickets);
                    Assert.Null(result.BestInningsRunsConceded);
                }
            }
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_Economy()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);
            var oversHelper = new OversHelper();

            foreach (var player in _databaseFixture.TestData.Players)
            {
                var result = await dataSource.ReadBowlingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                var dataForEconomy = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings)
                     .SelectMany(x => x.BowlingFigures.Where(o => o.Bowler.Player.PlayerId == player.PlayerId && o.RunsConceded.HasValue));
                var expectedEconomy = dataForEconomy.Sum(x => x.Overs) > 0 ? (decimal)dataForEconomy.Sum(x => x.RunsConceded.Value) / dataForEconomy.Sum(x => (decimal)oversHelper.OversToBallsBowled(x.Overs.Value) / StatisticsConstants.BALLS_PER_OVER) : (decimal?)null;

                Assert.NotNull(result);
                if (expectedEconomy.HasValue)
                {
                    Assert.NotNull(result.Economy);
                    Assert.Equal(expectedEconomy.Value.AccurateToTwoDecimalPlaces(), result.Economy.Value.AccurateToTwoDecimalPlaces());
                }
                else
                {
                    Assert.Null(result.Economy);
                }
            }
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_StrikeRate()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);
            var oversHelper = new OversHelper();

            foreach (var player in _databaseFixture.TestData.Players)
            {
                var result = await dataSource.ReadBowlingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                var dataForStrikeRate = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings)
                        .SelectMany(x => x.BowlingFigures.Where(o => o.Bowler.Player.PlayerId == player.PlayerId && o.Overs.HasValue));
                var expectedStrikeRate = dataForStrikeRate.Sum(x => x.Wickets) > 0 ? (decimal)dataForStrikeRate.Sum(x => oversHelper.OversToBallsBowled(x.Overs.Value)) / dataForStrikeRate.Sum(x => x.Wickets) : (decimal?)null;

                Assert.NotNull(result);
                if (expectedStrikeRate.HasValue)
                {
                    Assert.NotNull(result.StrikeRate);
                    Assert.Equal(expectedStrikeRate.Value.AccurateToTwoDecimalPlaces(), result.StrikeRate.Value.AccurateToTwoDecimalPlaces());
                }
                else
                {
                    Assert.Null(result.StrikeRate);
                }
            }
        }

        [Fact]
        public async Task Read_bowling_statistics_returns_Average()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            foreach (var player in _databaseFixture.TestData.Players)
            {
                var result = await dataSource.ReadBowlingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                var dataForAverage = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings)
                        .SelectMany(x => x.BowlingFigures.Where(o => o.Bowler.Player.PlayerId == player.PlayerId && o.RunsConceded.HasValue));
                var expectedAverage = dataForAverage.Sum(x => x.Wickets) > 0 ? (decimal)dataForAverage.Sum(x => x.RunsConceded) / dataForAverage.Sum(x => x.Wickets) : (decimal?)null;

                Assert.NotNull(result);
                if (expectedAverage.HasValue)
                {
                    Assert.NotNull(result.Average);
                    Assert.Equal(expectedAverage.Value.AccurateToTwoDecimalPlaces(), result.Average.Value.AccurateToTwoDecimalPlaces());
                }
                else
                {
                    Assert.Null(result.Average);
                }
            }
        }
    }
}
