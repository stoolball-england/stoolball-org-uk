using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Routing;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches
{
    [Collection(IntegrationTestConstants.DataSourceIntegrationTestCollection)]
    public class SqlServerTournamentDataSourceTests
    {
        private readonly SqlServerDataSourceFixture _databaseFixture;

        public SqlServerTournamentDataSourceTests(SqlServerDataSourceFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_tournament_by_route_reads_minimal_tournament_in_the_past()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TournamentInThePastWithMinimalDetails.TournamentRoute, "tournaments")).Returns(_databaseFixture.TournamentInThePastWithMinimalDetails.TournamentRoute);
            var tournamentDataSource = new SqlServerTournamentDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await tournamentDataSource.ReadTournamentByRoute(_databaseFixture.TournamentInThePastWithMinimalDetails.TournamentRoute).ConfigureAwait(false);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task Read_tournament_by_route_reads_minimal_tournament_in_the_future()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TournamentInTheFutureWithMinimalDetails.TournamentRoute, "tournaments")).Returns(_databaseFixture.TournamentInTheFutureWithMinimalDetails.TournamentRoute);
            var tournamentDataSource = new SqlServerTournamentDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await tournamentDataSource.ReadTournamentByRoute(_databaseFixture.TournamentInTheFutureWithMinimalDetails.TournamentRoute).ConfigureAwait(false);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task Read_tournament_by_route_returns_basic_tournament_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute, "tournaments")).Returns(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute);
            var tournamentDataSource = new SqlServerTournamentDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await tournamentDataSource.ReadTournamentByRoute(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentId, result.TournamentId);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentName, result.TournamentName);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.PlayerType, result.PlayerType);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.StartTime.LocalDateTime, result.StartTime.LocalDateTime);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.StartTimeIsKnown, result.StartTimeIsKnown);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.PlayersPerTeam, result.PlayersPerTeam);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.QualificationType, result.QualificationType);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.MaximumTeamsInTournament, result.MaximumTeamsInTournament);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.SpacesInTournament, result.SpacesInTournament);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentNotes, result.TournamentNotes);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute, result.TournamentRoute);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.MemberKey, result.MemberKey);
        }

        [Fact]
        public async Task Read_tournament_by_route_returns_teams_sorted_by_comparable_name()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute, "tournaments")).Returns(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute);
            var tournamentDataSource = new SqlServerTournamentDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await tournamentDataSource.ReadTournamentByRoute(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute).ConfigureAwait(false);

            var expectedTeams = _databaseFixture.TournamentInThePastWithFullDetails.Teams.OrderBy(x => x.Team.ComparableName()).ToList();
            for (var team = 0; team < expectedTeams.Count; team++)
            {
                Assert.Equal(expectedTeams[team].TeamRole, result.Teams[team].TeamRole);
                Assert.Equal(expectedTeams[team].Team.TeamId, result.Teams[team].Team.TeamId);
                Assert.Equal(expectedTeams[team].Team.TeamName, result.Teams[team].Team.TeamName);
                Assert.Equal(expectedTeams[team].Team.TeamRoute, result.Teams[team].Team.TeamRoute);
                Assert.Equal(expectedTeams[team].Team.TeamType, result.Teams[team].Team.TeamType);
            }
        }

        [Fact]
        public async Task Read_tournament_by_route_returns_over_sets()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute, "tournaments")).Returns(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute);
            var tournamentDataSource = new SqlServerTournamentDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await tournamentDataSource.ReadTournamentByRoute(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute).ConfigureAwait(false);

            for (var set = 0; set < _databaseFixture.TournamentInThePastWithFullDetails.DefaultOverSets.Count; set++)
            {
                Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.DefaultOverSets[set].OverSetId, result.DefaultOverSets[set].OverSetId);
                Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.DefaultOverSets[set].Overs, result.DefaultOverSets[set].Overs);
                Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.DefaultOverSets[set].BallsPerOver, result.DefaultOverSets[set].BallsPerOver);
            }
        }

        [Fact]
        public async Task Read_tournament_by_route_returns_tournament_location()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute, "tournaments")).Returns(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute);
            var tournamentDataSource = new SqlServerTournamentDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await tournamentDataSource.ReadTournamentByRoute(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentLocation.MatchLocationId, result.TournamentLocation.MatchLocationId);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentLocation.MatchLocationRoute, result.TournamentLocation.MatchLocationRoute);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentLocation.SecondaryAddressableObjectName, result.TournamentLocation.SecondaryAddressableObjectName);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentLocation.PrimaryAddressableObjectName, result.TournamentLocation.PrimaryAddressableObjectName);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentLocation.Locality, result.TournamentLocation.Locality);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentLocation.Town, result.TournamentLocation.Town);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentLocation.Latitude, result.TournamentLocation.Latitude);
            Assert.Equal(_databaseFixture.TournamentInThePastWithFullDetails.TournamentLocation.Longitude, result.TournamentLocation.Longitude);
        }

        [Fact]
        public async Task Read_tournament_by_route_returns_season_with_competition()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute, "tournaments")).Returns(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute);
            var tournamentDataSource = new SqlServerTournamentDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await tournamentDataSource.ReadTournamentByRoute(_databaseFixture.TournamentInThePastWithFullDetails.TournamentRoute).ConfigureAwait(false);

            foreach (var season in _databaseFixture.TournamentInThePastWithFullDetails.Seasons)
            {
                var resultSeason = result.Seasons.SingleOrDefault(x => x.SeasonId == season.SeasonId);
                Assert.NotNull(resultSeason);
                Assert.Equal(season.SeasonRoute, resultSeason.SeasonRoute);
                Assert.Equal(season.FromYear, resultSeason.FromYear);
                Assert.Equal(season.UntilYear, resultSeason.UntilYear);
                Assert.Equal(season.Competition.CompetitionName, resultSeason.Competition.CompetitionName);
            }
        }
    }
}
