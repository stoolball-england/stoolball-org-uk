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

#nullable disable
        [Fact]
        public async Task Read_batting_statistics_throws_ArgumentNullException_with_no_filter()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await dataSource.ReadBattingStatistics(null).ConfigureAwait(false)).ConfigureAwait(false);
        }
#nullable enable

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

        private async Task AssertBattingStatistics(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, Func<Stoolball.Matches.Match, bool> matchesFilter, Func<PlayerInnings, bool> playerInningsFilter)
        {
            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId!);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);
                var result = await dataSource.ReadBattingStatistics(filter).ConfigureAwait(false);

                Assert.NotNull(result);

                var expectedPlayerInnings = _databaseFixture.TestData
                    .Matches.Where(matchesFilter)
                    .SelectMany(x => x.MatchInnings)
                    .SelectMany(x => x.PlayerInnings).Where(x => x.Batter?.Player != null && x.Batter.Player.PlayerId == player.PlayerId).Where(playerInningsFilter);

                Assert.Equal(expectedPlayerInnings.Count(x => x.DismissalType != DismissalType.DidNotBat), result.TotalInnings);
                Assert.Equal(expectedPlayerInnings.Count(x => x.RunsScored.HasValue), result.TotalInningsWithRunsScored);
                Assert.Equal(expectedPlayerInnings.Count(x => x.RunsScored.HasValue && x.BallsFaced.HasValue), result.TotalInningsWithRunsScoredAndBallsFaced);
                Assert.Equal(expectedPlayerInnings.Count(x => x.DismissalType == DismissalType.NotOut || x.DismissalType == DismissalType.Retired || x.DismissalType == DismissalType.RetiredHurt), result.NotOuts);

                var totalRunsScored = expectedPlayerInnings.Where(x => x.RunsScored.HasValue).Sum(x => x.RunsScored);
                Assert.Equal(totalRunsScored, result.TotalRunsScored);
                Assert.Equal(expectedPlayerInnings.Count(x => x.RunsScored >= 50), result.Fifties);
                Assert.Equal(expectedPlayerInnings.Count(x => x.RunsScored >= 100), result.Hundreds);

                var bestRunsScored = expectedPlayerInnings.Max(x => x.RunsScored);
                if (bestRunsScored.HasValue)
                {
                    Assert.Equal(bestRunsScored, result.BestInningsRunsScored);
                    Assert.Equal(expectedPlayerInnings.Where(x => x.RunsScored == bestRunsScored)
                        .Any(x => x.DismissalType == DismissalType.NotOut || x.DismissalType == DismissalType.Retired || x.DismissalType == DismissalType.RetiredHurt), !result.BestInningsWasDismissed);
                }
                else
                {
                    Assert.Null(result.BestInningsRunsScored);
                }

                var totalRunsScoredWithBallsFaced = expectedPlayerInnings.Where(x => x.RunsScored.HasValue && x.BallsFaced.HasValue).Sum(x => x.RunsScored);
                var totalBallsFaced = expectedPlayerInnings.Where(x => x.RunsScored.HasValue && x.BallsFaced.HasValue).Sum(x => x.BallsFaced);
                if (totalRunsScoredWithBallsFaced.HasValue && totalBallsFaced > 0)
                {
                    var strikeRate = (((decimal)totalRunsScoredWithBallsFaced) / totalBallsFaced.Value) * 100;
                    Assert.Equal(Math.Round(strikeRate, 2), Math.Round(result.StrikeRate!.Value, 2));
                }
                else
                {
                    Assert.Null(result.StrikeRate);
                }

                var totalOuts = expectedPlayerInnings.Count(x => x.RunsScored.HasValue && x.DismissalType != DismissalType.NotOut && x.DismissalType != DismissalType.Retired && x.DismissalType != DismissalType.RetiredHurt);
                if (totalRunsScored.HasValue && totalOuts > 0)
                {
                    var average = ((decimal)totalRunsScored) / totalOuts;
                    Assert.Equal(Math.Round(average, 2), Math.Round(result.Average!.Value, 2));
                }
                else
                {
                    Assert.Null(result.Average);
                }
            }
        }

        [Fact]
        public async Task Read_batting_statistics_supports_no_filter()
        {
            await AssertBattingStatistics(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                x => true, x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_supports_filter_by_date()
        {
            var (fromDate, untilDate) = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await AssertBattingStatistics(new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate },
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", fromDate }, { "UntilDate", untilDate } },
               x => x.StartTime >= fromDate && x.StartTime <= untilDate,
               x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_batting_statistics_supports_filter_by_team_id()
        {
            await AssertBattingStatistics(new StatisticsFilter { Team = _databaseFixture.TestData.TeamWithFullDetails },
               " AND TeamId = @TeamId",
               new Dictionary<string, object> { { "TeamId", _databaseFixture.TestData.TeamWithFullDetails!.TeamId!.Value } },
               x => x.Teams.Select(t => t.Team?.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId),
               x => x.Batter?.Team != null && x.Batter.Team.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId).ConfigureAwait(false);
        }
    }
}
