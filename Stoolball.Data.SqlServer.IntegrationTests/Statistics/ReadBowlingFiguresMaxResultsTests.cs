using System.Transactions;
using Stoolball.Statistics;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class ReadBowlingFiguresMaxResultsTests : IDisposable
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly TransactionScope _scope;

        public ReadBowlingFiguresMaxResultsTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        public void Dispose() => _scope.Dispose();

        [Fact]
        public async Task Read_bowling_figures_with_MaxResultsAllowingExtraResultsIfValuesAreEqual_returns_results_equal_to_the_max()
        {
            var (player, bowlingFigures) = await ForceFifthAndSixthBowlingFiguresToBeTheSame().ConfigureAwait(false);

            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, new StatisticsQueryBuilder());

            var results = (await dataSource.ReadBowlingFigures(new StatisticsFilter
            {
                MaxResultsAllowingExtraResultsIfValuesAreEqual = 5,
                Player = player
            },
            StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();

            var allExpectedResults = bowlingFigures
                .OrderByDescending(x => x.Wickets).ThenByDescending(x => x.RunsConceded.HasValue).ThenBy(x => x.RunsConceded);

            var expected = new List<BowlingFigures>();
            foreach (var result in allExpectedResults)
            {
                if (expected.Count < 5 ||
                    (expected[expected.Count - 1].Wickets == result.Wickets && expected[expected.Count - 1].RunsConceded == result.RunsConceded))
                {
                    expected.Add(result);
                    continue;
                }
                else break;
            }

            Assert.Equal(expected.Count, results.Count);
            foreach (var expectedFigures in expected)
            {
                var result = results.SingleOrDefault(x => x.Result.BowlingFiguresId == expectedFigures.BowlingFiguresId);
                Assert.NotNull(result);

                Assert.Equal(expectedFigures.Overs, result!.Result.Overs);
                Assert.Equal(expectedFigures.Maidens, result.Result.Maidens);
                Assert.Equal(expectedFigures.RunsConceded, result.Result.RunsConceded);
                Assert.Equal(expectedFigures.Wickets, result.Result.Wickets);
            }
            Assert.Equal(results[4].Result.Wickets, results[5].Result.Wickets);
            Assert.Equal(results[4].Result.RunsConceded, results[5].Result.RunsConceded);
        }

        /// <summary>
        /// Finds a player with at least six sets of bowling figures in the shared test data, then updates the pre-computed
        /// statistics for those figures in the database so that the sixth-best set matches the fifth, letting us test retrieving
        /// a top five plus any equal results. The update only happens inside this test's transaction, so it's rolled back
        /// afterwards and the shared test data seen by other tests is untouched.
        /// </summary>
        private async Task<(Player Player, List<BowlingFigures> BowlingFigures)> ForceFifthAndSixthBowlingFiguresToBeTheSame()
        {
            var originalFiguresInOrder = _databaseFixture.TestData.MatchesThatCouldHavePlayerStatistics()
                .SelectMany(m => m.MatchInnings)
                .SelectMany(mi => mi.BowlingFigures)
                .GroupBy(x => x.Bowler!.Player!.PlayerId)
                .First(g => g.Count() > 5)
                .OrderByDescending(x => x.Wickets).ThenByDescending(x => x.RunsConceded.HasValue).ThenBy(x => x.RunsConceded)
                .ToList();

            var player = originalFiguresInOrder[0].Bowler!.Player!;

            // Clone the figures so we can compute new values without mutating the shared fixture's cached test data,
            // which is reused by many other tests.
            var bowlingFigures = originalFiguresInOrder.Select(x => new BowlingFigures
            {
                BowlingFiguresId = x.BowlingFiguresId,
                Overs = x.Overs,
                Maidens = x.Maidens,
                RunsConceded = x.RunsConceded,
                Wickets = x.Wickets
            }).ToList();

            // Make the sixth set of figures the same as the fifth.
            bowlingFigures[5].Overs = bowlingFigures[4].Overs;
            bowlingFigures[5].Maidens = bowlingFigures[4].Maidens;
            bowlingFigures[5].RunsConceded = bowlingFigures[4].RunsConceded;
            bowlingFigures[5].Wickets = bowlingFigures[4].Wickets;

            // If there are more than six sets of figures, make sure they're worse so we know what to assert.
            for (var i = 6; i < bowlingFigures.Count; i++)
            {
                bowlingFigures[i].RunsConceded = (bowlingFigures[i].RunsConceded ?? 0) + 1;
            }

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                foreach (var figures in bowlingFigures)
                {
                    await connection.ExecuteAsync(
                        $@"UPDATE {Tables.PlayerInMatchStatistics}
                           SET Overs = @Overs, Maidens = @Maidens, RunsConceded = @RunsConceded, HasRunsConceded = @HasRunsConceded, Wickets = @Wickets
                           WHERE BowlingFiguresId = @BowlingFiguresId",
                        new
                        {
                            figures.Overs,
                            figures.Maidens,
                            figures.RunsConceded,
                            HasRunsConceded = figures.RunsConceded != null,
                            figures.Wickets,
                            figures.BowlingFiguresId
                        }).ConfigureAwait(false);
                }
            }

            return (player, bowlingFigures);
        }
    }
}
