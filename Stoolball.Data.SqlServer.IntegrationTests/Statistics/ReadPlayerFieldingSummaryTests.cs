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
    public class ReadPlayerFieldingSummaryTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;

        public ReadPlayerFieldingSummaryTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_fielding_statistics_throws_ArgumentNullException_with_no_filter()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await dataSource.ReadFieldingStatistics(null).ConfigureAwait(false)).ConfigureAwait(false);
        }

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

        [Fact]
        public async Task Read_fielding_statistics_returns_TotalCatches()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            foreach (var player in _databaseFixture.TestData.Players)
            {
                var result = await dataSource.ReadFieldingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Equal(_databaseFixture.TestData.PlayerInnings.Count(x => (x.DismissalType == DismissalType.Caught && x.DismissedBy != null && x.DismissedBy.Player.PlayerId == player.PlayerId) ||
                                                                                (x.DismissalType == DismissalType.CaughtAndBowled && x.Bowler != null && x.Bowler.Player.PlayerId == player.PlayerId)), result.TotalCatches);
            }
        }

        [Fact]
        public async Task Read_fielding_statistics_returns_TotalRunOuts()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            foreach (var player in _databaseFixture.TestData.Players)
            {
                var result = await dataSource.ReadFieldingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                Assert.Equal(_databaseFixture.TestData.PlayerInnings.Count(x => x.DismissalType == DismissalType.RunOut && x.DismissedBy != null && x.DismissedBy.Player.PlayerId == player.PlayerId), result.TotalRunOuts);
            }
        }

        [Fact]
        public async Task Read_fielding_statistics_returns_MostCatches()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            foreach (var player in _databaseFixture.TestData.Players)
            {
                var result = await dataSource.ReadFieldingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                var best = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings)
                                                            .Where(i => i.PlayerInnings.Any(x => (x.DismissalType == DismissalType.Caught && x.DismissedBy != null && x.DismissedBy.Player.PlayerId == player.PlayerId) ||
                                                                                                 (x.DismissalType == DismissalType.CaughtAndBowled && x.Bowler != null && x.Bowler.Player.PlayerId == player.PlayerId)))
                                                            .Select(i => i.PlayerInnings.Count(x => (x.DismissalType == DismissalType.Caught && x.DismissedBy != null && x.DismissedBy.Player.PlayerId == player.PlayerId) ||
                                                                                                    (x.DismissalType == DismissalType.CaughtAndBowled && x.Bowler != null && x.Bowler.Player.PlayerId == player.PlayerId)))
                                                            .OrderByDescending(x => x)
                                                            .FirstOrDefault();

                Assert.Equal(best, result.MostCatches);
            }
        }

        [Fact]
        public async Task Read_fielding_statistics_returns_MostRunOuts()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            var dataSource = new SqlServerPlayerSummaryStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            foreach (var player in _databaseFixture.TestData.Players)
            {
                var result = await dataSource.ReadFieldingStatistics(new StatisticsFilter { Player = player }).ConfigureAwait(false);

                var best = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings)
                                                              .Where(i => i.PlayerInnings.Any(x => x.DismissalType == DismissalType.RunOut && x.DismissedBy != null && x.DismissedBy.Player.PlayerId == player.PlayerId))
                                                              .Select(i => i.PlayerInnings.Count(x => x.DismissalType == DismissalType.RunOut && x.DismissedBy != null && x.DismissedBy.Player.PlayerId == player.PlayerId))
                                                              .OrderByDescending(x => x)
                                                              .FirstOrDefault();

                Assert.Equal(best, result.MostRunOuts);
            }
        }
    }
}
