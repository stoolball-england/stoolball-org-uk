using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.StatisticsMaxResultsDataSourceIntegrationTestCollection)]
    public class ReadBowlingFiguresMaxResultsTests
    {
        private readonly SqlServerStatisticsMaxResultsDataSourceFixture _databaseFixture;

        public ReadBowlingFiguresMaxResultsTests(SqlServerStatisticsMaxResultsDataSourceFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_bowling_figures_with_MaxResultsAllowingExtraResultsIfValuesAreEqual_returns_results_equal_to_the_max()
        {
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, new StatisticsQueryBuilder());

            var results = (await dataSource.ReadBowlingFigures(new StatisticsFilter
            {
                MaxResultsAllowingExtraResultsIfValuesAreEqual = 5,
                Player = _databaseFixture.PlayerWithFifthAndSixthBowlingFiguresTheSame
            },
            StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();

            var allExpectedResults = _databaseFixture.TestData.BowlingFigures
                .Where(x => x.Bowler.Player.PlayerId == _databaseFixture.PlayerWithFifthAndSixthBowlingFiguresTheSame.PlayerId)
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
    }
}
