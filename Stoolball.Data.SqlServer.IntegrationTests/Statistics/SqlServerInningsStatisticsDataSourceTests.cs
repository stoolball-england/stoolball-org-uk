using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Statistics;
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

        [Fact]
        public async Task Read_innings_statistics_supports_no_filter()
        {
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));
            var dataSource = new SqlServerInningsStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var result = await dataSource.ReadInningsStatistics(null).ConfigureAwait(false);

            var innings = _databaseFixture.TestData.Matches.SelectMany(x => x.MatchInnings);
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

            foreach (var team in _databaseFixture.TestData.Teams)
            {
                var filter = new StatisticsFilter { Team = team };
                var queryBuilder = new Mock<IStatisticsQueryBuilder>();
                queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND TeamId = @TeamId", new Dictionary<string, object> { { "TeamId", filter.Team.TeamId } }));
                var dataSource = new SqlServerInningsStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

                var result = await dataSource.ReadInningsStatistics(filter).ConfigureAwait(false);

                var matchesForTeam = _databaseFixture.TestData.Matches.Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(team.TeamId.Value));
                var inningsForTeam = matchesForTeam.SelectMany(m => m.MatchInnings.Where(x => x.BattingTeam.Team.TeamId == team.TeamId.Value));

                if (inningsForTeam.Any())
                {
                    var expectedAverageRuns = ((decimal?)inningsForTeam.Where(x => x.Runs.HasValue).Average(x => x.Runs))?.AccurateToTwoDecimalPlaces();
                    var expectedHighestRuns = inningsForTeam.Where(x => x.Runs.HasValue).Max(x => x.Runs);
                    var expectedLowestRuns = inningsForTeam.Where(x => x.Runs.HasValue).Min(x => x.Runs);
                    var expectedAverageWickets = ((decimal?)inningsForTeam.Where(x => x.Wickets.HasValue).Average(x => x.Wickets))?.AccurateToTwoDecimalPlaces();

                    Assert.Equal(expectedAverageRuns, result.AverageRunsScored.Value.AccurateToTwoDecimalPlaces());
                    Assert.Equal(expectedHighestRuns, result.HighestRunsScored.Value);
                    Assert.Equal(expectedLowestRuns, result.LowestRunsScored.Value);
                    Assert.Equal(expectedAverageWickets, result.AverageWicketsLost.Value.AccurateToTwoDecimalPlaces());
                }

                var inningsForOpposition = matchesForTeam.SelectMany(m => m.MatchInnings.Where(x => x.BowlingTeam.Team.TeamId == team.TeamId.Value));

                if (inningsForOpposition.Any())
                {
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

        [Fact]
        public async Task Read_innings_statistics_supports_filter_by_club()
        {
            var filter = new StatisticsFilter { Club = _databaseFixture.TestData.TeamWithFullDetails.Club };
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND ClubId = @ClubId", new Dictionary<string, object> { { "ClubId", _databaseFixture.TestData.TeamWithFullDetails.Club.ClubId } }));
            var dataSource = new SqlServerInningsStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var result = await dataSource.ReadInningsStatistics(filter).ConfigureAwait(false);

            var matchesForClub = _databaseFixture.TestData.Matches.Where(x => x.Teams.Select(t => t.Team.TeamId).Contains(_databaseFixture.TestData.TeamWithFullDetails.TeamId.Value));
            var inningsForClub = matchesForClub.SelectMany(m => m.MatchInnings.Where(x => x.BattingTeam.Team.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId.Value));

            var expectedAverageRuns = ((decimal)inningsForClub.Average(x => x.Runs)).AccurateToTwoDecimalPlaces();
            var expectedHighestRuns = inningsForClub.Where(x => x.Runs.HasValue).Max(x => x.Runs);
            var expectedLowestRuns = inningsForClub.Where(x => x.Runs.HasValue).Min(x => x.Runs);
            var expectedAverageWickets = ((decimal)inningsForClub.Where(x => x.Wickets.HasValue).Average(x => x.Wickets)).AccurateToTwoDecimalPlaces();

            Assert.Equal(expectedAverageRuns, result.AverageRunsScored.Value.AccurateToTwoDecimalPlaces());
            Assert.Equal(expectedHighestRuns, result.HighestRunsScored.Value);
            Assert.Equal(expectedLowestRuns, result.LowestRunsScored.Value);
            Assert.Equal(expectedAverageWickets, result.AverageWicketsLost.Value.AccurateToTwoDecimalPlaces());

            var inningsForOpposition = matchesForClub.SelectMany(m => m.MatchInnings.Where(x => x.BowlingTeam.Team.TeamId == _databaseFixture.TestData.TeamWithFullDetails.TeamId.Value));
            var expectedOppositionAverageRuns = ((decimal)inningsForOpposition.Average(x => x.Runs)).AccurateToTwoDecimalPlaces();
            var expectedOppositionHighestRuns = inningsForOpposition.Where(x => x.Runs.HasValue).Max(x => x.Runs);
            var expectedOppositionLowestRuns = inningsForOpposition.Where(x => x.Runs.HasValue).Min(x => x.Runs);
            var expectedOppositionAverageWickets = ((decimal)inningsForOpposition.Where(x => x.Wickets.HasValue).Average(x => x.Wickets)).AccurateToTwoDecimalPlaces();

            Assert.Equal(expectedOppositionAverageRuns, result.AverageRunsConceded.Value.AccurateToTwoDecimalPlaces());
            Assert.Equal(expectedOppositionHighestRuns, result.HighestRunsConceded.Value);
            Assert.Equal(expectedOppositionLowestRuns, result.LowestRunsConceded.Value);
            Assert.Equal(expectedOppositionAverageWickets, result.AverageWicketsTaken.Value.AccurateToTwoDecimalPlaces());
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
            var queryBuilder = new Mock<IStatisticsQueryBuilder>();
            queryBuilder.Setup(x => x.BuildWhereClause(filter)).Returns((" AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate", new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } }));
            var dataSource = new SqlServerInningsStatisticsDataSource(_databaseFixture.ConnectionFactory, queryBuilder.Object);

            var result = await dataSource.ReadInningsStatistics(filter).ConfigureAwait(false);

            var inningsMatchingFilter = _databaseFixture.TestData.Matches.Where(x => x.StartTime >= filter.FromDate && x.StartTime <= filter.UntilDate).SelectMany(m => m.MatchInnings);

            var expectedAverageRuns = ((decimal)inningsMatchingFilter.Average(x => x.Runs)).AccurateToTwoDecimalPlaces();
            var expectedHighestRuns = inningsMatchingFilter.Where(x => x.Runs.HasValue).Max(x => x.Runs);
            var expectedLowestRuns = inningsMatchingFilter.Where(x => x.Runs.HasValue).Min(x => x.Runs);
            var expectedAverageWickets = ((decimal)inningsMatchingFilter.Where(x => x.Wickets.HasValue).Average(x => x.Wickets)).AccurateToTwoDecimalPlaces();

            Assert.Equal(expectedAverageRuns, result.AverageRunsScored.Value.AccurateToTwoDecimalPlaces());
            Assert.Equal(expectedHighestRuns, result.HighestRunsScored.Value);
            Assert.Equal(expectedLowestRuns, result.LowestRunsScored.Value);
            Assert.Equal(expectedAverageWickets, result.AverageWicketsLost.Value.AccurateToTwoDecimalPlaces());

            // With no team filter opposition results should be the same, because all teams are considered in both calculations
            Assert.Equal(expectedAverageRuns, result.AverageRunsConceded.Value.AccurateToTwoDecimalPlaces());
            Assert.Equal(expectedHighestRuns, result.HighestRunsConceded.Value);
            Assert.Equal(expectedLowestRuns, result.LowestRunsConceded.Value);
            Assert.Equal(expectedAverageWickets, result.AverageWicketsTaken.Value.AccurateToTwoDecimalPlaces());
        }
    }
}
