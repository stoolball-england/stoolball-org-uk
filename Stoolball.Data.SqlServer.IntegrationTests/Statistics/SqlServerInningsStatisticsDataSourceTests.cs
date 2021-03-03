using System;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.StatisticsDataSourceIntegrationTestCollection)]
    public class SqlServerInningsStatisticsDataSourceTests
    {
        private readonly SqlServerStatisticsDataSourceFixture _databaseFixture;

        public SqlServerInningsStatisticsDataSourceTests(SqlServerStatisticsDataSourceFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_innings_statistics_supports_no_filter()
        {
            var dataSource = new SqlServerInningsStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var result = await dataSource.ReadInningsStatistics(null).ConfigureAwait(false);

            var innings = _databaseFixture.Matches.SelectMany(x => x.MatchInnings);
            var expectedAverageRuns = ((decimal)innings.Where(x => x.Runs.HasValue).Average(x => x.Runs)).AccurateToTwoDecimalPlaces();
            var expectedHighestRuns = innings.Where(x => x.Runs.HasValue).Max(x => x.Runs);
            var expectedLowestRuns = innings.Where(x => x.Runs.HasValue).Min(x => x.Runs);
            var expectedAverageWickets = ((decimal)innings.Where(x => x.Wickets.HasValue).Average(x => x.Wickets)).AccurateToTwoDecimalPlaces();

            Assert.Equal(expectedAverageRuns, result.AverageRunsScored.Value.AccurateToTwoDecimalPlaces());
            Assert.Equal(expectedHighestRuns, result.HighestRunsScored.Value);
            Assert.Equal(expectedLowestRuns, result.LowestRunsScored.Value);
            Assert.Equal(expectedAverageWickets, result.AverageWicketsLost.Value.AccurateToTwoDecimalPlaces());

            // With no filter opposition results should be the same, because all teams are considered in both calculations
            Assert.Equal(expectedAverageRuns, result.AverageRunsConceded.Value.AccurateToTwoDecimalPlaces());
            Assert.Equal(expectedHighestRuns, result.HighestRunsConceded.Value);
            Assert.Equal(expectedLowestRuns, result.LowestRunsConceded.Value);
            Assert.Equal(expectedAverageWickets, result.AverageWicketsTaken.Value.AccurateToTwoDecimalPlaces());
        }

        [Fact]
        public async Task Read_innings_statistics_supports_filter_by_team()
        {
            var dataSource = new SqlServerInningsStatisticsDataSource(_databaseFixture.ConnectionFactory);

            foreach (var team in _databaseFixture.Teams)
            {
                var result = await dataSource.ReadInningsStatistics(new StatisticsFilter { Team = team }).ConfigureAwait(false);

                var matchesForTeam = _databaseFixture.Matches.Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(team.TeamId.Value));
                var inningsForTeam = matchesForTeam.SelectMany(m => m.MatchInnings.Where(x => x.BattingTeam.Team.TeamId == team.TeamId.Value));

                var expectedAverageRuns = ((decimal)inningsForTeam.Average(x => x.Runs)).AccurateToTwoDecimalPlaces();
                var expectedHighestRuns = inningsForTeam.Where(x => x.Runs.HasValue).Max(x => x.Runs);
                var expectedLowestRuns = inningsForTeam.Where(x => x.Runs.HasValue).Min(x => x.Runs);
                var expectedAverageWickets = ((decimal)inningsForTeam.Where(x => x.Wickets.HasValue).Average(x => x.Wickets)).AccurateToTwoDecimalPlaces();

                Assert.Equal(expectedAverageRuns, result.AverageRunsScored.Value.AccurateToTwoDecimalPlaces());
                Assert.Equal(expectedHighestRuns, result.HighestRunsScored.Value);
                Assert.Equal(expectedLowestRuns, result.LowestRunsScored.Value);
                Assert.Equal(expectedAverageWickets, result.AverageWicketsLost.Value.AccurateToTwoDecimalPlaces());

                var inningsForOpposition = matchesForTeam.SelectMany(m => m.MatchInnings.Where(x => x.BattingTeam.Team.TeamId != team.TeamId.Value));
                var expectedOppositionAverageRuns = ((decimal)inningsForOpposition.Average(x => x.Runs)).AccurateToTwoDecimalPlaces();
                var expectedOppositionHighestRuns = inningsForOpposition.Where(x => x.Runs.HasValue).Max(x => x.Runs);
                var expectedOppositionLowestRuns = inningsForOpposition.Where(x => x.Runs.HasValue).Min(x => x.Runs);
                var expectedOppositionAverageWickets = ((decimal)inningsForOpposition.Where(x => x.Wickets.HasValue).Average(x => x.Wickets)).AccurateToTwoDecimalPlaces();

                Assert.Equal(expectedOppositionAverageRuns, result.AverageRunsConceded.Value.AccurateToTwoDecimalPlaces());
                Assert.Equal(expectedOppositionHighestRuns, result.HighestRunsConceded.Value);
                Assert.Equal(expectedOppositionLowestRuns, result.LowestRunsConceded.Value);
                Assert.Equal(expectedOppositionAverageWickets, result.AverageWicketsTaken.Value.AccurateToTwoDecimalPlaces());

            }
        }

        [Fact]
        public async Task Read_innings_statistics_supports_filter_by_club()
        {
            var dataSource = new SqlServerInningsStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var result = await dataSource.ReadInningsStatistics(new StatisticsFilter { Club = _databaseFixture.TeamWithClub.Club }).ConfigureAwait(false);

            var matchesForClub = _databaseFixture.Matches.Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TeamWithClub.TeamId.Value));
            var inningsForClub = matchesForClub.SelectMany(m => m.MatchInnings.Where(x => x.BattingTeam.Team.TeamId == _databaseFixture.TeamWithClub.TeamId.Value));

            var expectedAverageRuns = ((decimal)inningsForClub.Average(x => x.Runs)).AccurateToTwoDecimalPlaces();
            var expectedHighestRuns = inningsForClub.Where(x => x.Runs.HasValue).Max(x => x.Runs);
            var expectedLowestRuns = inningsForClub.Where(x => x.Runs.HasValue).Min(x => x.Runs);
            var expectedAverageWickets = ((decimal)inningsForClub.Where(x => x.Wickets.HasValue).Average(x => x.Wickets)).AccurateToTwoDecimalPlaces();

            Assert.Equal(expectedAverageRuns, result.AverageRunsScored.Value.AccurateToTwoDecimalPlaces());
            Assert.Equal(expectedHighestRuns, result.HighestRunsScored.Value);
            Assert.Equal(expectedLowestRuns, result.LowestRunsScored.Value);
            Assert.Equal(expectedAverageWickets, result.AverageWicketsLost.Value.AccurateToTwoDecimalPlaces());

            var inningsForOpposition = matchesForClub.SelectMany(m => m.MatchInnings.Where(x => x.BattingTeam.Team.TeamId != _databaseFixture.TeamWithClub.TeamId.Value));
            var expectedOppositionAverageRuns = ((decimal)inningsForOpposition.Average(x => x.Runs)).AccurateToTwoDecimalPlaces();
            var expectedOppositionHighestRuns = inningsForOpposition.Where(x => x.Runs.HasValue).Max(x => x.Runs);
            var expectedOppositionLowestRuns = inningsForOpposition.Where(x => x.Runs.HasValue).Min(x => x.Runs);
            var expectedOppositionAverageWickets = ((decimal)inningsForOpposition.Where(x => x.Wickets.HasValue).Average(x => x.Wickets)).AccurateToTwoDecimalPlaces();

            Assert.Equal(expectedOppositionAverageRuns, result.AverageRunsConceded.Value.AccurateToTwoDecimalPlaces());
            Assert.Equal(expectedOppositionHighestRuns, result.HighestRunsConceded.Value);
            Assert.Equal(expectedOppositionLowestRuns, result.LowestRunsConceded.Value);
            Assert.Equal(expectedOppositionAverageWickets, result.AverageWicketsTaken.Value.AccurateToTwoDecimalPlaces());
        }
    }
}
