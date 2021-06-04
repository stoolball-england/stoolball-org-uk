using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class ReadPlayerPerformancePlayerInningsTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;

        public ReadPlayerPerformancePlayerInningsTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_player_innings_returns_batter()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerPlayerPerformanceStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).ToList();
            foreach (var expectedInnings in expected)
            {
                var result = results.SingleOrDefault(x => x.Result.PlayerInningsId == expectedInnings.PlayerInningsId);
                Assert.NotNull(result);

                Assert.Equal(expectedInnings.Batter.Player.PlayerRoute, result.Result.Batter.Player.PlayerRoute);
                Assert.Equal(expectedInnings.Batter.PlayerIdentityName, result.Result.Batter.PlayerIdentityName);
            }
        }

        [Fact]
        public async Task Read_player_innings_returns_innings()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerPlayerPerformanceStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).ToList();
            foreach (var expectedInnings in expected)
            {
                var result = results.SingleOrDefault(x => x.Result.PlayerInningsId == expectedInnings.PlayerInningsId);
                Assert.NotNull(result);

                Assert.Equal(expectedInnings.DismissalType, result.Result.DismissalType);
                Assert.Equal(expectedInnings.RunsScored, result.Result.RunsScored);
            }
        }


        [Fact]
        public async Task Read_player_innings_returns_bowler()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerPlayerPerformanceStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).ToList();
            foreach (var expectedInnings in expected)
            {
                var result = results.SingleOrDefault(x => x.Result.PlayerInningsId == expectedInnings.PlayerInningsId);
                Assert.NotNull(result);

                Assert.Equal(expectedInnings.Bowler?.Player.PlayerRoute, result.Result.Bowler?.Player.PlayerRoute);
                Assert.Equal(expectedInnings.Bowler?.PlayerIdentityName, result.Result.Bowler?.PlayerIdentityName);
            }
        }

        [Fact]
        public async Task Read_player_innings_returns_match()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerPlayerPerformanceStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter).ConfigureAwait(false);

            foreach (var match in _databaseFixture.TestData.Matches)
            {
                foreach (var innings in match.MatchInnings)
                {
                    foreach (var playerInnings in innings.PlayerInnings)
                    {
                        var result = results.SingleOrDefault(x => x.Result.PlayerInningsId == playerInnings.PlayerInningsId);
                        Assert.NotNull(result);

                        Assert.Equal(match.MatchRoute, result.Match.MatchRoute);
                        Assert.Equal(match.StartTime.AccurateToTheMinute(), result.Match.StartTime.AccurateToTheMinute());
                        Assert.Equal(match.MatchName, result.Match.MatchName);
                    }
                }
            }
        }

        [Fact]
        public async Task Read_player_innings_supports_no_filter()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerPlayerPerformanceStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).ToList();

            Assert.Equal(expected.Count, results.Count());
            foreach (var expectedInnings in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Result.PlayerInningsId == expectedInnings.PlayerInningsId));
            }
        }

        [Fact]
        public async Task Read_player_innings_supports_filter_by_catcher_id()
        {
            foreach (var player in _databaseFixture.TestData.PlayersWithMultipleIdentities)
            {
                var filter = new StatisticsFilter
                {
                    Paging = new Paging
                    {
                        PageSize = int.MaxValue
                    },
                    CaughtByPlayerIdentityIds = player.PlayerIdentities.Select(x => x.PlayerIdentityId.Value).ToList()
                };
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND CaughtByPlayerIdentityId IN @PlayerIdentities", new Dictionary<string, object> { { "PlayerIdentities", filter.CaughtByPlayerIdentityIds } }));
                var dataSource = new SqlServerPlayerPerformanceStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

                var results = await dataSource.ReadPlayerInnings(filter).ConfigureAwait(false);

                var expected = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings)
                    .SelectMany(x => x.PlayerInnings)
                    .Where(x => (x.DismissalType == DismissalType.Caught && x.DismissedBy?.Player.PlayerId == player.PlayerId) ||
                                (x.DismissalType == DismissalType.CaughtAndBowled && x.Bowler?.Player.PlayerId == player.PlayerId)).ToList();
                Assert.Equal(expected.Count, results.Count());
                foreach (var expectedInnings in expected)
                {
                    Assert.NotNull(results.SingleOrDefault(x => x.Result.PlayerInningsId == expectedInnings.PlayerInningsId));
                }
            }
        }

        [Fact]
        public async Task Read_player_innings_supports_filter_by_run_out_fielder_id()
        {
            foreach (var player in _databaseFixture.TestData.PlayersWithMultipleIdentities)
            {
                var filter = new StatisticsFilter
                {
                    Paging = new Paging
                    {
                        PageSize = int.MaxValue
                    },
                    RunOutByPlayerIdentityIds = player.PlayerIdentities.Select(x => x.PlayerIdentityId.Value).ToList()
                };
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND RunOutByPlayerIdentityId IN @PlayerIdentities", new Dictionary<string, object> { { "PlayerIdentities", filter.RunOutByPlayerIdentityIds } }));
                var dataSource = new SqlServerPlayerPerformanceStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

                var results = await dataSource.ReadPlayerInnings(filter).ConfigureAwait(false);

                var expected = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings)
                    .SelectMany(x => x.PlayerInnings)
                    .Where(x => x.DismissalType == DismissalType.RunOut && x.DismissedBy?.Player.PlayerId == player.PlayerId).ToList();
                Assert.Equal(expected.Count, results.Count());
                foreach (var expectedInnings in expected)
                {
                    Assert.NotNull(results.SingleOrDefault(x => x.Result.PlayerInningsId == expectedInnings.PlayerInningsId));
                }
            }
        }

        [Fact]
        public async Task Read_player_innings_sorts_by_most_recent_first()
        {
            var filter = new StatisticsFilter { Paging = new Paging { PageSize = int.MaxValue } };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerPlayerPerformanceStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = await dataSource.ReadPlayerInnings(filter).ConfigureAwait(false);

            var previousInningsStartTime = DateTimeOffset.MaxValue;
            foreach (var result in results)
            {
                Assert.True(result.Match.StartTime.AccurateToTheMinute() <= previousInningsStartTime);
                previousInningsStartTime = result.Match.StartTime.AccurateToTheMinute();
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
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((string.Empty, new Dictionary<string, object>()));
                var dataSource = new SqlServerPlayerPerformanceStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);
                var results = await dataSource.ReadPlayerInnings(filter).ConfigureAwait(false);

                var expected = pageSize > remaining ? remaining : pageSize;
                Assert.Equal(expected, results.Count());

                pageNumber++;
                remaining -= pageSize;
            }
        }
    }
}
