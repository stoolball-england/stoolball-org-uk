using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Statistics;
using Stoolball.Testing;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class ReadPlayerPerformancePlayerInningsTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly Mock<IStatisticsQueryBuilder> _queryBuilder = new();

        public ReadPlayerPerformancePlayerInningsTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_player_innings_returns_batter()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerPlayerPerformanceStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).ToList();
            foreach (var expectedInnings in expected)
            {
                var result = results.SingleOrDefault(x => x.Result?.PlayerInningsId == expectedInnings.PlayerInningsId);
                Assert.NotNull(result?.Result?.Batter?.Player);

                Assert.Equal(expectedInnings.Batter!.Player!.PlayerRoute, result!.Result!.Batter!.Player!.PlayerRoute);
                Assert.Equal(expectedInnings.Batter.PlayerIdentityName, result.Result.Batter.PlayerIdentityName);
            }
        }

        [Fact]
        public async Task Read_player_innings_returns_innings()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerPlayerPerformanceStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).ToList();
            foreach (var expectedInnings in expected)
            {
                var result = results.SingleOrDefault(x => x.Result?.PlayerInningsId == expectedInnings.PlayerInningsId);
                Assert.NotNull(result?.Result);

                Assert.Equal(expectedInnings.DismissalType, result!.Result!.DismissalType);
                Assert.Equal(expectedInnings.RunsScored, result.Result.RunsScored);
            }
        }


        [Fact]
        public async Task Read_player_innings_returns_bowler()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerPlayerPerformanceStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).ToList();
            foreach (var expectedInnings in expected)
            {
                var result = results.SingleOrDefault(x => x.Result?.PlayerInningsId == expectedInnings.PlayerInningsId);
                Assert.NotNull(result?.Result);

                Assert.Equal(expectedInnings.Bowler?.Player?.PlayerRoute, result!.Result!.Bowler?.Player?.PlayerRoute);
                Assert.Equal(expectedInnings.Bowler?.PlayerIdentityName, result.Result.Bowler?.PlayerIdentityName);
            }
        }

        [Fact]
        public async Task Read_player_innings_returns_match()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerPlayerPerformanceStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter).ConfigureAwait(false);

            foreach (var match in _databaseFixture.TestData.Matches)
            {
                foreach (var innings in match.MatchInnings)
                {
                    foreach (var playerInnings in innings.PlayerInnings)
                    {
                        var result = results.SingleOrDefault(x => x.Result?.PlayerInningsId == playerInnings.PlayerInningsId);
                        Assert.NotNull(result?.Match);

                        Assert.Equal(match.MatchRoute, result!.Match!.MatchRoute);
                        Assert.Equal(match.StartTime, result.Match.StartTime);
                        Assert.Equal(match.MatchName, result.Match.MatchName);
                    }
                }
            }
        }

        private async Task AssertFilteredResults(StatisticsFilter filter, Func<Stoolball.Matches.Match, bool> matchesFilter, Func<MatchInnings, bool> matchInningsFilter, Func<PlayerInnings, bool> playerInningsFilter)
        {
            filter.Paging = new Paging
            {
                PageSize = int.MaxValue
            };

            var dataSource = new SqlServerPlayerPerformanceStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.Where(matchesFilter)
                .SelectMany(x => x.MatchInnings).Where(matchInningsFilter)
                .SelectMany(x => x.PlayerInnings).Where(playerInningsFilter)
                .ToList();
            Assert.Equal(expected.Count, results.Count());
            foreach (var expectedInnings in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result?.PlayerInningsId == expectedInnings.PlayerInningsId));
            }
        }

        [Fact]
        public async Task Read_player_innings_supports_no_filter()
        {
            var filter = new StatisticsFilter();
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            await AssertFilteredResults(filter, x => true, x => true, x => true).ConfigureAwait(false);
        }

        [Fact]
        public async Task Read_player_innings_supports_filter_by_catcher_id()
        {
            foreach (var player in _databaseFixture.TestData.PlayersWithMultipleIdentities)
            {
                var filter = new StatisticsFilter
                {
                    CaughtByPlayerIdentityIds = player.PlayerIdentities.Select(x => x.PlayerIdentityId!.Value).ToList()
                };
                _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND CaughtByPlayerIdentityId IN @PlayerIdentities", new Dictionary<string, object> { { "PlayerIdentities", filter.CaughtByPlayerIdentityIds } }));

                await AssertFilteredResults(filter, x => true, x => true,
                    x => (x.DismissalType == DismissalType.Caught && x.DismissedBy?.Player?.PlayerId == player.PlayerId) ||
                         (x.DismissalType == DismissalType.CaughtAndBowled && x.Bowler?.Player?.PlayerId == player.PlayerId)).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task Read_player_innings_supports_filter_by_run_out_fielder_id()
        {
            foreach (var player in _databaseFixture.TestData.PlayersWithMultipleIdentities)
            {
                var filter = new StatisticsFilter
                {
                    RunOutByPlayerIdentityIds = player.PlayerIdentities.Select(x => x.PlayerIdentityId!.Value).ToList()
                };
                _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND RunOutByPlayerIdentityId IN @PlayerIdentities", new Dictionary<string, object> { { "PlayerIdentities", filter.RunOutByPlayerIdentityIds } }));

                await AssertFilteredResults(filter, x => true, x => true,
                    x => x.DismissalType == DismissalType.RunOut && x.DismissedBy?.Player?.PlayerId == player.PlayerId
                ).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task Read_player_innings_supports_filter_by_date()
        {
            var dateRangeGenerator = new DateRangeGenerator();

            foreach (var player in _databaseFixture.TestData.PlayersWithMultipleIdentities)
            {
                var (fromDate, untilDate) = dateRangeGenerator.SelectDateRangeToTest(_databaseFixture.TestData.Matches);
                var filter = new StatisticsFilter
                {
                    FromDate = fromDate,
                    UntilDate = untilDate,
                };
                _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate", new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } }));

                await AssertFilteredResults(filter,
                    x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate,
                    x => true,
                    x => true).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task Read_player_innings_supports_filter_by_batting_team_id()
        {
            foreach (var player in _databaseFixture.TestData.PlayersWithMultipleIdentities)
            {
                var filter = new StatisticsFilter
                {
                    Team = _databaseFixture.TestData.TeamWithFullDetails
                };
                _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND TeamId = @TeamId", new Dictionary<string, object> { { "TeamId", filter.Team!.TeamId! } }));

                await AssertFilteredResults(filter,
                    x => x.Teams.Select(t => t.Team?.TeamId).Contains(filter.Team.TeamId),
                    x => x.BattingTeam?.Team?.TeamId == filter.Team.TeamId,
                    x => true
                ).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task Read_player_innings_supports_filter_by_fielding_team_id()
        {
            foreach (var player in _databaseFixture.TestData.PlayersWithMultipleIdentities)
            {
                var filter = new StatisticsFilter
                {
                    Team = _databaseFixture.TestData.TeamWithFullDetails,
                    SwapTeamAndOppositionFilters = true,
                };
                _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND OppositionTeamId = @OppositionTeamId", new Dictionary<string, object> { { "OppositionTeamId", filter.Team!.TeamId! } }));

                await AssertFilteredResults(filter,
                    x => x.Teams.Select(t => t.Team?.TeamId).Contains(filter.Team.TeamId),
                    x => x.BowlingTeam?.Team?.TeamId == filter.Team.TeamId,
                    x => true
                    ).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task Read_player_innings_sorts_by_most_recent_first()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerPlayerPerformanceStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter).ConfigureAwait(false);

            DateTimeOffset? previousInningsStartTime = DateTimeOffset.MaxValue;
            foreach (var result in results)
            {
                Assert.True(result.Match?.StartTime <= previousInningsStartTime);
                previousInningsStartTime = result.Match?.StartTime;
            }
        }

        [Fact]
        public async Task Read_player_innings_pages_results()
        {
            const int pageSize = 10;
            var pageNumber = 1;
            var remaining = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).Count();
            while (remaining > 0)
            {
                var filter = new StatisticsFilter { Paging = new Paging { PageNumber = pageNumber, PageSize = pageSize } };
                _queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
                var dataSource = new SqlServerPlayerPerformanceStatisticsDataSource(_databaseFixture.ConnectionFactory, _queryBuilder.Object);
                var results = await dataSource.ReadPlayerInnings(filter).ConfigureAwait(false);

                var expected = pageSize > remaining ? remaining : pageSize;
                Assert.Equal(expected, results.Count());

                pageNumber++;
                remaining -= pageSize;
            }
        }
    }
}
