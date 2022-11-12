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
    public class ReadPlayerBowlingSummaryTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly DateRangeGenerator _dateRangeGenerator = new DateRangeGenerator();

        public ReadPlayerBowlingSummaryTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

#nullable disable
        [Fact]
        public async Task Read_bowling_statistics_throws_ArgumentNullException_with_no_filter()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await dataSource.ReadBowlingStatistics(null).ConfigureAwait(false)).ConfigureAwait(false);
        }
#nullable enable

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

        private async Task TestBowlingStatistics(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, Func<Stoolball.Matches.Match, bool> matchesFilter, Func<PlayerIdentity?, bool> bowlerFilter)
        {
            Func<PlayerIdentity?, Player, bool> actualBowlerFilter = (bowler, player) => bowler?.Player?.PlayerId == player.PlayerId && bowlerFilter(bowler);

            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId!);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);
                var oversHelper = new OversHelper();

                var result = await dataSource.ReadBowlingStatistics(filter).ConfigureAwait(false);

                Assert.NotNull(result);

                var expectedMatchInnings = _databaseFixture.TestData
                    .Matches.Where(matchesFilter)
                    .SelectMany(x => x.MatchInnings);

                Assert.Equal(expectedMatchInnings.Count(x =>
                        x.OversBowled.Any(o => actualBowlerFilter(o.Bowler, player)) ||
                        x.PlayerInnings.Any(pi => actualBowlerFilter(pi.Bowler, player))
                    ), result.TotalInnings);

                Assert.Equal(expectedMatchInnings.Count(x =>
                        x.OversBowled.Any(o => actualBowlerFilter(o.Bowler, player) && o.RunsConceded.HasValue)),
                        result.TotalInningsWithRunsConceded);

                Assert.Equal(expectedMatchInnings.Count(x =>
                        x.BowlingFigures.Any(o => actualBowlerFilter(o.Bowler, player) && o.Overs.HasValue)),
                        result.TotalInningsWithBallsBowled);

                var ballsBowled = expectedMatchInnings
                        .SelectMany(x => x.BowlingFigures.Where(o => actualBowlerFilter(o.Bowler, player) && o.Overs.HasValue))
                        .Sum(o => oversHelper.OversToBallsBowled(o.Overs!.Value));

                Assert.Equal(oversHelper.BallsBowledToOvers(ballsBowled), result.TotalOvers);

                var maidens = expectedMatchInnings
                        .SelectMany(x => x.BowlingFigures.Where(o => actualBowlerFilter(o.Bowler, player) && o.Maidens.HasValue))
                        .Sum(x => x.Maidens);

                Assert.Equal(maidens, result.TotalMaidens);

                var runsConceded = expectedMatchInnings
                       .SelectMany(x => x.BowlingFigures.Where(o => actualBowlerFilter(o.Bowler, player) && o.RunsConceded.HasValue))
                       .Sum(x => x.RunsConceded);

                Assert.Equal(runsConceded, result.TotalRunsConceded);

                var wickets = expectedMatchInnings
                       .SelectMany(x => x.BowlingFigures.Where(o => actualBowlerFilter(o.Bowler, player)))
                       .Sum(x => x.Wickets);

                Assert.Equal(wickets, result.TotalWickets);

                var wicketHauls = expectedMatchInnings
                        .SelectMany(x => x.BowlingFigures.Where(o => actualBowlerFilter(o.Bowler, player)))
                        .Count(x => x.Wickets >= 5);

                Assert.Equal(wicketHauls, result.FiveWicketInnings);

                var best = expectedMatchInnings // get all innings...
                .SelectMany(x => x.BowlingFigures.Where(o => actualBowlerFilter(o.Bowler, player))) // ...where this player bowled, and select their bowling figures
                .GroupBy(x => x.MatchInnings?.MatchInningsId, x => x, (matchInningsId, bowlingFigures) => new BowlingFigures // combine any multiple identities for the player into new bowling figures
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
                    Assert.Equal(best.Wickets, result.BestInningsWickets);

                    if (best.RunsConceded.HasValue)
                    {
                        Assert.Equal(best.RunsConceded, result.BestInningsRunsConceded);
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

                var dataForEconomy = expectedMatchInnings
                     .SelectMany(x => x.BowlingFigures.Where(o => actualBowlerFilter(o.Bowler, player) && o.RunsConceded.HasValue));
                var expectedEconomy = dataForEconomy.Sum(x => x.Overs) > 0 ? (decimal?)dataForEconomy.Sum(x => x.RunsConceded) / dataForEconomy.Sum(x => (decimal)oversHelper.OversToBallsBowled(x.Overs!.Value) / StatisticsConstants.BALLS_PER_OVER) : (decimal?)null;

                if (expectedEconomy.HasValue)
                {
                    Assert.NotNull(result.Economy);
                    Assert.Equal(expectedEconomy.Value.AccurateToTwoDecimalPlaces(), result.Economy!.Value.AccurateToTwoDecimalPlaces());
                }
                else
                {
                    Assert.Null(result.Economy);
                }

                var dataForStrikeRate = expectedMatchInnings
                        .SelectMany(x => x.BowlingFigures.Where(o => actualBowlerFilter(o.Bowler, player) && o.Overs.HasValue));
                var expectedStrikeRate = dataForStrikeRate.Sum(x => x.Wickets) > 0 ? (decimal)dataForStrikeRate.Sum(x => oversHelper.OversToBallsBowled(x.Overs!.Value)) / dataForStrikeRate.Sum(x => x.Wickets) : (decimal?)null;

                if (expectedStrikeRate.HasValue)
                {
                    Assert.NotNull(result.StrikeRate);
                    Assert.Equal(expectedStrikeRate.Value.AccurateToTwoDecimalPlaces(), result.StrikeRate!.Value.AccurateToTwoDecimalPlaces());
                }
                else
                {
                    Assert.Null(result.StrikeRate);
                }

                var dataForAverage = expectedMatchInnings
                        .SelectMany(x => x.BowlingFigures.Where(o => actualBowlerFilter(o.Bowler, player) && o.RunsConceded.HasValue));
                var expectedAverage = dataForAverage.Sum(x => x.Wickets) > 0 ? (decimal?)dataForAverage.Sum(x => x.RunsConceded) / dataForAverage.Sum(x => x.Wickets) : (decimal?)null;

                if (expectedAverage.HasValue)
                {
                    Assert.NotNull(result.Average);
                    Assert.Equal(expectedAverage.Value.AccurateToTwoDecimalPlaces(), result.Average!.Value.AccurateToTwoDecimalPlaces());
                }
                else
                {
                    Assert.Null(result.Average);
                }
            }
        }

        [Fact]
        public async Task Read_bowling_statistics_supports_no_filter()
        {
            await TestBowlingStatistics(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                x => true, x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_supports_filter_by_date()
        {
            var (fromDate, untilDate) = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await TestBowlingStatistics(new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate },
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", fromDate }, { "UntilDate", untilDate } },
               x => x.StartTime >= fromDate && x.StartTime <= untilDate,
               x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_supports_filter_by_team_id()
        {
            await TestBowlingStatistics(new StatisticsFilter { Team = _databaseFixture.TestData.TeamWithFullDetails },
               " AND TeamId = @TeamId",
               new Dictionary<string, object> { { "TeamId", _databaseFixture.TestData.TeamWithFullDetails!.TeamId!.Value } },
               x => x.Teams.Select(t => t.Team?.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId),
               x => x?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_bowling_statistics_supports_filter_by_team_route()
        {
            await TestBowlingStatistics(new StatisticsFilter { Team = _databaseFixture.TestData.TeamWithFullDetails },
               " AND TeamRoute = @TeamRoute",
               new Dictionary<string, object> { { "TeamRoute", _databaseFixture.TestData.TeamWithFullDetails!.TeamRoute! } },
               x => x.Teams.Select(t => t.Team?.TeamRoute).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamRoute),
               x => x?.Team?.TeamRoute == _databaseFixture.TestData.TeamWithFullDetails.TeamRoute).ConfigureAwait(false);
        }
    }
}
