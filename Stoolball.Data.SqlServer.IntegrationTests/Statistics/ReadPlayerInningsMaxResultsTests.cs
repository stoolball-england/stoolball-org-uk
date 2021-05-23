using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Matches;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.StatisticsMaxResultsDataSourceIntegrationTestCollection)]
    public class ReadPlayerInningsMaxResultsTests
    {
        private readonly SqlServerStatisticsMaxResultsDataSourceFixture _databaseFixture;

        public ReadPlayerInningsMaxResultsTests(SqlServerStatisticsMaxResultsDataSourceFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_player_innings_with_MaxResultsAllowingExtraResultsIfValuesAreEqual_returns_results_equal_to_the_max()
        {
            var filter = new StatisticsFilter
            {
                MaxResultsAllowingExtraResultsIfValuesAreEqual = 5,
                Player = _databaseFixture.PlayerWithFifthAndSixthInningsTheSame
            };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns(("AND PlayerId = @PlayerId", new Dictionary<string, object> { { "PlayerId", _databaseFixture.PlayerWithFifthAndSixthInningsTheSame.PlayerId } }));
            var dataSource = new SqlServerBestPerformanceInAMatchStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var results = (await dataSource.ReadPlayerInnings(filter, StatisticsSortOrder.BestFirst).ConfigureAwait(false)).ToList();

            var allExpectedResults = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.PlayerInnings)
                .Where(x => x.Batter.Player.PlayerId == _databaseFixture.PlayerWithFifthAndSixthInningsTheSame.PlayerId && x.RunsScored.HasValue)
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

                Assert.Equal(expectedInnings.DismissalType, result.Result.DismissalType);
                Assert.Equal(expectedInnings.RunsScored, result.Result.RunsScored);
                Assert.Equal(expectedInnings.BallsFaced, result.Result.BallsFaced);
            }
            Assert.Equal(results[4].Result.RunsScored, results[5].Result.RunsScored);
        }
    }
}
