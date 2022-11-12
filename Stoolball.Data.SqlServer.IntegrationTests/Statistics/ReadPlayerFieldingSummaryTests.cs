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

#nullable disable
        [Fact]
        public async Task Read_fielding_statistics_throws_ArgumentNullException_with_no_filter()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await dataSource.ReadFieldingStatistics(null).ConfigureAwait(false)).ConfigureAwait(false);
        }
#nullable enable

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

        private async Task AssertFieldingStatistics(StatisticsFilter filter, string whereClause, Dictionary<string, object> parameters, Func<Stoolball.Matches.Match, bool> matchesFilter, Func<PlayerIdentity?, bool> fielderFilter)
        {
            Func<PlayerIdentity?, Player, bool> actualFielderFilter = (fielder, player) => fielder?.Player?.PlayerId == player.PlayerId && fielderFilter(fielder);

            foreach (var player in _databaseFixture.TestData.Players)
            {
                filter.Player = player;
                parameters.Remove("PlayerId");
                parameters.Add("PlayerId", player.PlayerId!);
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND PlayerId = @PlayerId" + whereClause, parameters));
                var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

                var result = await dataSource.ReadFieldingStatistics(filter).ConfigureAwait(false);

                Assert.NotNull(result);

                var expectedMatchInnings = _databaseFixture.TestData
                    .Matches.Where(matchesFilter)
                    .SelectMany(x => x.MatchInnings);

                Assert.Equal(expectedMatchInnings.SelectMany(x => x.PlayerInnings)
                    .Count(x => (x.DismissalType == DismissalType.Caught && actualFielderFilter(x.DismissedBy, player)) ||
                                (x.DismissalType == DismissalType.CaughtAndBowled && actualFielderFilter(x.Bowler, player))), result.TotalCatches);

                Assert.Equal(expectedMatchInnings.SelectMany(x => x.PlayerInnings)
                    .Count(x => x.DismissalType == DismissalType.RunOut && actualFielderFilter(x.DismissedBy, player)), result.TotalRunOuts);

                var mostCatches = expectedMatchInnings
                                    .Where(i => i.PlayerInnings.Any(x => (x.DismissalType == DismissalType.Caught && actualFielderFilter(x.DismissedBy, player)) ||
                                                                            (x.DismissalType == DismissalType.CaughtAndBowled && actualFielderFilter(x.Bowler, player))))
                                    .Select(i => i.PlayerInnings.Count(x => (x.DismissalType == DismissalType.Caught && actualFielderFilter(x.DismissedBy, player)) ||
                                                                            (x.DismissalType == DismissalType.CaughtAndBowled && actualFielderFilter(x.Bowler, player))))
                                    .OrderByDescending(x => x)
                                    .FirstOrDefault();

                Assert.Equal(mostCatches, result.MostCatches);


                var mostRunOuts = expectedMatchInnings
                                    .Where(i => i.PlayerInnings.Any(x => x.DismissalType == DismissalType.RunOut && actualFielderFilter(x.DismissedBy, player)))
                                    .Select(i => i.PlayerInnings.Count(x => x.DismissalType == DismissalType.RunOut && actualFielderFilter(x.DismissedBy, player)))
                                    .OrderByDescending(x => x)
                                    .FirstOrDefault();

                Assert.Equal(mostRunOuts, result.MostRunOuts);
            }
        }

        [Fact]
        public async Task Read_fielding_statistics_supports_no_filter()
        {
            await AssertFieldingStatistics(new StatisticsFilter(),
                string.Empty,
                new Dictionary<string, object>(),
                x => true,
                x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_fielding_statistics_supports_filter_by_date()
        {
            var (fromDate, untilDate) = _dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
            await AssertFieldingStatistics(new StatisticsFilter { FromDate = fromDate, UntilDate = untilDate },
               " AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate",
               new Dictionary<string, object> { { "FromDate", fromDate }, { "UntilDate", untilDate } },
               x => x.StartTime >= fromDate && x.StartTime <= untilDate,
               x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_fielding_statistics_supports_filter_by_team_id()
        {
            await AssertFieldingStatistics(new StatisticsFilter { Team = _databaseFixture.TestData.TeamWithFullDetails },
               " AND TeamId = @TeamId",
               new Dictionary<string, object> { { "TeamId", _databaseFixture.TestData.TeamWithFullDetails!.TeamId!.Value } },
               x => x.Teams.Select(t => t.Team?.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId),
               x => x?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_fielding_statistics_supports_filter_by_team_route()
        {
            await AssertFieldingStatistics(new StatisticsFilter { Team = _databaseFixture.TestData.TeamWithFullDetails },
               " AND TeamRoute = @TeamRoute",
               new Dictionary<string, object> { { "TeamRoute", _databaseFixture.TestData.TeamWithFullDetails!.TeamRoute! } },
               x => x.Teams.Select(t => t.Team?.TeamRoute).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamRoute),
               x => x?.Team?.TeamRoute == _databaseFixture.TestData.TeamWithFullDetails.TeamRoute).ConfigureAwait(false);
        }
    }
}
