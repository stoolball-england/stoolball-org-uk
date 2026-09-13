using System.Transactions;
using Stoolball.Statistics;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class ReadPlayerInningsMaxResultsTests : IDisposable
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly TransactionScope _scope;

        public ReadPlayerInningsMaxResultsTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        public void Dispose() => _scope.Dispose();

        [Fact]
        public async Task Read_player_innings_with_MaxResultsAllowingExtraResultsIfValuesAreEqual_returns_results_equal_to_the_max()
        {
            var (player, playerInnings) = await ForceFifthAndSixthPlayerInningsToBeTheSame().ConfigureAwait(false);

            var filter = new StatisticsFilter
            {
                MaxResultsAllowingExtraResultsIfValuesAreEqual = 5,
                Player = player
            };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns(("AND PlayerId = @PlayerId", new Dictionary<string, object> { { "PlayerId", player.PlayerId! } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = (await dataSource.ReadPlayerInnings(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();

            var allExpectedResults = playerInnings
                .OrderByDescending(x => x.RunsScored).ThenBy(x => StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(x.DismissalType));

            var expected = new List<PlayerInnings>();
            foreach (var result in allExpectedResults)
            {
                if (expected.Count < 5 ||
                    (expected[expected.Count - 1].RunsScored == result.RunsScored && StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(expected[expected.Count - 1].DismissalType) == StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(result.DismissalType)))
                {
                    expected.Add(result);
                    continue;
                }
                else break;
            }

            Assert.Equal(expected.Count, results.Count);
            foreach (var expectedInnings in expected)
            {
                var result = results.SingleOrDefault(x => x.Result.PlayerInningsId == expectedInnings.PlayerInningsId);
                Assert.NotNull(result);

                Assert.Equal(expectedInnings.DismissalType, result!.Result.DismissalType);
                Assert.Equal(expectedInnings.RunsScored, result.Result.RunsScored);
                Assert.Equal(expectedInnings.BallsFaced, result.Result.BallsFaced);
            }
            Assert.Equal(results[4].Result.RunsScored, results[5].Result.RunsScored);
        }

        /// <summary>
        /// Finds a player with at least six qualifying innings in the shared test data, then updates the pre-computed statistics
        /// for those innings in the database so that the sixth-best score matches the fifth, letting us test retrieving a top five
        /// plus any equal results. The update only happens inside this test's transaction, so it's rolled back afterwards and the
        /// shared test data seen by other tests is untouched.
        /// </summary>
        private async Task<(Player Player, List<PlayerInnings> PlayerInnings)> ForceFifthAndSixthPlayerInningsToBeTheSame()
        {
            var originalInningsInOrder = _databaseFixture.TestData.MatchesThatCouldHavePlayerStatistics()
                .SelectMany(m => m.MatchInnings)
                .SelectMany(mi => mi.PlayerInnings)
                .Where(i => i.DismissalType != DismissalType.DidNotBat && i.DismissalType != DismissalType.TimedOut && i.RunsScored.HasValue)
                .GroupBy(i => i.Batter!.Player!.PlayerId)
                .First(g => g.Count() > 5)
                .OrderByDescending(i => i.RunsScored)
                .ToList();

            var player = originalInningsInOrder[0].Batter!.Player!;

            // Clone the innings so we can compute new values without mutating the shared fixture's cached test data,
            // which is reused by many other tests.
            var playerInnings = originalInningsInOrder.Select(i => new PlayerInnings
            {
                PlayerInningsId = i.PlayerInningsId,
                RunsScored = i.RunsScored,
                DismissalType = i.DismissalType,
                BallsFaced = i.BallsFaced
            }).ToList();

            // Make the sixth innings the same as the fifth, including anything that might affect the out/not out status.
            playerInnings[5].DismissalType = playerInnings[4].DismissalType;
            playerInnings[5].RunsScored = playerInnings[4].RunsScored;
            playerInnings[5].BallsFaced = playerInnings[4].BallsFaced;

            // The assertion expects the fifth and sixth innings to be the same, but to be different than any that come before or
            // after in the result set. So make sure those others are different.

            // Step 1: Make room below if required
            if (playerInnings.Count > 6 && playerInnings[5].RunsScored == 0)
            {
                playerInnings[4].RunsScored++;
                playerInnings[5].RunsScored++;
            }

            // Step 2: Ensure earlier scores are higher
            for (var i = 0; i < 4; i++)
            {
                if (playerInnings[i].RunsScored == playerInnings[4].RunsScored)
                {
                    playerInnings[i].RunsScored++;
                }
            }

            // Step 3: Ensure later scores are lower, but not below 0
            for (var i = 6; i < playerInnings.Count; i++)
            {
                playerInnings[i].RunsScored = playerInnings[i].RunsScored > 0 ? playerInnings[i].RunsScored - 1 : 0;
            }

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                foreach (var innings in playerInnings)
                {
                    await connection.ExecuteAsync(
                        $@"UPDATE {Tables.PlayerInMatchStatistics}
                           SET RunsScored = @RunsScored, BallsFaced = @BallsFaced, DismissalType = @DismissalType, PlayerWasDismissed = @PlayerWasDismissed
                           WHERE PlayerInningsId = @PlayerInningsId",
                        new
                        {
                            innings.RunsScored,
                            innings.BallsFaced,
                            innings.DismissalType,
                            PlayerWasDismissed = StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(innings.DismissalType),
                            innings.PlayerInningsId
                        }).ConfigureAwait(false);
                }
            }

            return (player, playerInnings);
        }
    }
}
