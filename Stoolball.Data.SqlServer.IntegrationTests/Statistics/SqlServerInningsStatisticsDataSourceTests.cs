using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerInningsStatisticsDataSourceTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;

        public SqlServerInningsStatisticsDataSourceTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        private void AssertInningsStatistics(IEnumerable<MatchInnings> battingInnings, IEnumerable<MatchInnings> bowlingInnings, InningsStatistics result)
        {
            if (battingInnings.Any())
            {
                var hasInningsWithRuns = battingInnings.Any(x => x.Runs.HasValue);
                var hasInningsWithWickets = battingInnings.Any(x => x.Wickets.HasValue);
                var expectedAverageRuns = hasInningsWithRuns ? ((decimal?)battingInnings.Where(x => x.Runs.HasValue).Average(x => x.Runs!.Value))?.AccurateToTwoDecimalPlaces() : null;
                var expectedHighestRuns = hasInningsWithRuns ? battingInnings.Where(x => x.Runs.HasValue).Max(x => x.Runs) : null;
                var expectedLowestRuns = hasInningsWithRuns ? battingInnings.Where(x => x.Runs.HasValue).Min(x => x.Runs) : null;
                var expectedAverageWickets = hasInningsWithWickets ? ((decimal?)battingInnings.Where(x => x.Wickets.HasValue).Average(x => x.Wickets!.Value))?.AccurateToTwoDecimalPlaces() : null;

                Assert.Equal(expectedAverageRuns, result.AverageRunsScored?.AccurateToTwoDecimalPlaces());
                Assert.Equal(expectedHighestRuns, result.HighestRunsScored);
                Assert.Equal(expectedLowestRuns, result.LowestRunsScored);
                Assert.Equal(expectedAverageWickets, result.AverageWicketsLost?.AccurateToTwoDecimalPlaces());
            }

            if (bowlingInnings.Any())
            {
                var hasInningsWithRuns = bowlingInnings.Any(x => x.Runs.HasValue);
                var hasInningsWithWickets = bowlingInnings.Any(x => x.Wickets.HasValue);
                var expectedOppositionAverageRuns = hasInningsWithRuns ? ((decimal?)bowlingInnings.Where(x => x.Runs.HasValue).Average(x => x.Runs!.Value))?.AccurateToTwoDecimalPlaces() : null;
                var expectedOppositionHighestRuns = hasInningsWithRuns ? bowlingInnings.Where(x => x.Runs.HasValue).Max(x => x.Runs) : null;
                var expectedOppositionLowestRuns = hasInningsWithRuns ? bowlingInnings.Where(x => x.Runs.HasValue).Min(x => x.Runs) : null;
                var expectedOppositionAverageWickets = hasInningsWithWickets ? ((decimal?)bowlingInnings.Where(x => x.Wickets.HasValue).Average(x => x.Wickets!.Value))?.AccurateToTwoDecimalPlaces() : null;

                Assert.Equal(expectedOppositionAverageRuns, result.AverageRunsConceded?.AccurateToTwoDecimalPlaces());
                Assert.Equal(expectedOppositionHighestRuns, result.HighestRunsConceded);
                Assert.Equal(expectedOppositionLowestRuns, result.LowestRunsConceded);
                Assert.Equal(expectedOppositionAverageWickets, result.AverageWicketsTaken?.AccurateToTwoDecimalPlaces());
            }
        }

        [Fact]
        public async Task Read_innings_statistics_supports_no_filter()
        {
            var dataSource = new SqlServerInningsStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var result = await dataSource.ReadInningsStatistics(null).ConfigureAwait(false);

            var innings = _databaseFixture.TestData.MatchInnings;

            // With no filter opposition results should be the same, because all teams are considered in both calculations
            AssertInningsStatistics(innings, innings, result);
        }

        [Fact]
        public async Task Read_innings_statistics_supports_filter_by_team_id()
        {
            foreach (var team in _databaseFixture.TestData.Teams)
            {
                var filter = new StatisticsFilter { Team = new Team { TeamId = team.TeamId } };
                var dataSource = new SqlServerInningsStatisticsDataSource(_databaseFixture.ConnectionFactory);

                var result = await dataSource.ReadInningsStatistics(filter).ConfigureAwait(false);

                var matchesForTeam = _databaseFixture.TestData.Matches.Where(x => x.Teams.Select(t => t.Team!.TeamId).Contains(team.TeamId));
                var inningsForTeam = matchesForTeam.SelectMany(m => m.MatchInnings.Where(x => x.BattingTeam?.Team?.TeamId == team.TeamId));
                var inningsForOpposition = matchesForTeam.SelectMany(m => m.MatchInnings.Where(x => x.BowlingTeam?.Team?.TeamId == team.TeamId));

                AssertInningsStatistics(inningsForTeam, inningsForOpposition, result);
            }
        }


        [Fact]
        public async Task Read_innings_statistics_supports_filter_by_team_route()
        {
            foreach (var team in _databaseFixture.TestData.Teams)
            {
                var filter = new StatisticsFilter { Team = new Team { TeamRoute = team.TeamRoute } };
                var dataSource = new SqlServerInningsStatisticsDataSource(_databaseFixture.ConnectionFactory);

                var result = await dataSource.ReadInningsStatistics(filter).ConfigureAwait(false);

                var matchesForTeam = _databaseFixture.TestData.Matches.Where(x => x.Teams.Select(t => t.Team!.TeamRoute).Contains(team.TeamRoute));
                var inningsForTeam = matchesForTeam.SelectMany(m => m.MatchInnings.Where(x => x.BattingTeam?.Team?.TeamRoute == team.TeamRoute));
                var inningsForOpposition = matchesForTeam.SelectMany(m => m.MatchInnings.Where(x => x.BowlingTeam?.Team?.TeamRoute == team.TeamRoute));

                AssertInningsStatistics(inningsForTeam, inningsForOpposition, result);
            }
        }

        [Fact]
        public async Task Read_innings_statistics_supports_filter_by_club()
        {
            var filter = new StatisticsFilter { Club = _databaseFixture.TestData.TeamWithFullDetails!.Club };
            var dataSource = new SqlServerInningsStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var result = await dataSource.ReadInningsStatistics(filter).ConfigureAwait(false);

            var matchesForClub = _databaseFixture.TestData.Matches.Where(x => x.Teams.Select(t => t.Team!.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId));
            var inningsForClub = matchesForClub.SelectMany(m => m.MatchInnings.Where(x => x.BattingTeam?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId));
            var inningsForOpposition = matchesForClub.SelectMany(m => m.MatchInnings.Where(x => x.BowlingTeam?.Team?.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId));

            AssertInningsStatistics(inningsForClub, inningsForOpposition, result);
        }

        [Fact]
        public async Task Read_innings_statistics_supports_filter_by_date()
        {
            var allMatchDates = _databaseFixture.TestData.Matches.Select(x => x.StartTime).OrderBy(x => x);
            var oneThirdOfTheTimeBetweenFirstAndLast = (allMatchDates.Last() - allMatchDates.First()) / 3;
            var filter = new StatisticsFilter
            {
                FromDate = allMatchDates.First().Add(oneThirdOfTheTimeBetweenFirstAndLast),
                UntilDate = allMatchDates.Last().Add(-oneThirdOfTheTimeBetweenFirstAndLast)
            };
            var dataSource = new SqlServerInningsStatisticsDataSource(_databaseFixture.ConnectionFactory);

            var result = await dataSource.ReadInningsStatistics(filter).ConfigureAwait(false);

            var inningsMatchingFilter = _databaseFixture.TestData.Matches.Where(x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate).SelectMany(m => m.MatchInnings);

            // With no team filter opposition results should be the same, because all teams are considered in both calculations
            AssertInningsStatistics(inningsMatchingFilter, inningsMatchingFilter, result);
        }
    }
}
