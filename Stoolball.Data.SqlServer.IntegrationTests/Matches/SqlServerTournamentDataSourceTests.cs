using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Routing;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerTournamentDataSourceTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;

        public SqlServerTournamentDataSourceTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_tournament_by_route_reads_minimal_tournament_in_the_past()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.TournamentInThePastWithMinimalDetails!.TournamentRoute, "tournaments")).Returns(_databaseFixture.TestData.TournamentInThePastWithMinimalDetails!.TournamentRoute);
            var tournamentDataSource = new SqlServerTournamentDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await tournamentDataSource.ReadTournamentByRoute(_databaseFixture.TestData.TournamentInThePastWithMinimalDetails.TournamentRoute).ConfigureAwait(false);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task Read_tournament_by_route_reads_minimal_tournament_in_the_future()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.TournamentInTheFutureWithMinimalDetails!.TournamentRoute, "tournaments")).Returns(_databaseFixture.TestData.TournamentInTheFutureWithMinimalDetails!.TournamentRoute);
            var tournamentDataSource = new SqlServerTournamentDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await tournamentDataSource.ReadTournamentByRoute(_databaseFixture.TestData.TournamentInTheFutureWithMinimalDetails.TournamentRoute).ConfigureAwait(false);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task Read_tournament_by_route_returns_basic_tournament_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.TournamentInThePastWithFullDetails!.TournamentRoute, "tournaments")).Returns(_databaseFixture.TestData.TournamentInThePastWithFullDetails!.TournamentRoute);
            var tournamentDataSource = new SqlServerTournamentDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await tournamentDataSource.ReadTournamentByRoute(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentRoute).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentId, result!.TournamentId);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentName, result.TournamentName);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.PlayerType, result.PlayerType);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.StartTime, result.StartTime);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.StartTimeIsKnown, result.StartTimeIsKnown);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.PlayersPerTeam, result.PlayersPerTeam);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.QualificationType, result.QualificationType);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.MaximumTeamsInTournament, result.MaximumTeamsInTournament);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.SpacesInTournament, result.SpacesInTournament);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentNotes, result.TournamentNotes);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentRoute, result.TournamentRoute);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.MemberKey, result.MemberKey);
        }

        [Fact]
        public async Task Read_tournament_by_route_returns_teams_sorted_by_comparable_name()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.TournamentInThePastWithFullDetails!.TournamentRoute, "tournaments")).Returns(_databaseFixture.TestData.TournamentInThePastWithFullDetails!.TournamentRoute);
            var tournamentDataSource = new SqlServerTournamentDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await tournamentDataSource.ReadTournamentByRoute(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentRoute).ConfigureAwait(false);
            Assert.NotNull(result);

            var expectedTeams = _databaseFixture.TestData.TournamentInThePastWithFullDetails.Teams.OrderBy(x => x.Team.ComparableName()).ToList();
            for (var team = 0; team < expectedTeams.Count; team++)
            {
                Assert.Equal(expectedTeams[team].TournamentTeamId, result!.Teams[team].TournamentTeamId);
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
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.TournamentInThePastWithFullDetails!.TournamentRoute, "tournaments")).Returns(_databaseFixture.TestData.TournamentInThePastWithFullDetails!.TournamentRoute);
            var tournamentDataSource = new SqlServerTournamentDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await tournamentDataSource.ReadTournamentByRoute(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentRoute).ConfigureAwait(false);
            Assert.NotNull(result);

            for (var set = 0; set < _databaseFixture.TestData.TournamentInThePastWithFullDetails.DefaultOverSets.Count; set++)
            {
                Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.DefaultOverSets[set].OverSetId, result!.DefaultOverSets[set].OverSetId);
                Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.DefaultOverSets[set].Overs, result.DefaultOverSets[set].Overs);
                Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.DefaultOverSets[set].BallsPerOver, result.DefaultOverSets[set].BallsPerOver);
            }
        }

        [Fact]
        public async Task Read_tournament_by_route_returns_tournament_location()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.TournamentInThePastWithFullDetails!.TournamentRoute, "tournaments")).Returns(_databaseFixture.TestData.TournamentInThePastWithFullDetails!.TournamentRoute);
            var tournamentDataSource = new SqlServerTournamentDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await tournamentDataSource.ReadTournamentByRoute(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentRoute).ConfigureAwait(false);
            Assert.NotNull(result);

            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentLocation.MatchLocationId, result!.TournamentLocation.MatchLocationId);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentLocation.MatchLocationRoute, result.TournamentLocation.MatchLocationRoute);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentLocation.SecondaryAddressableObjectName, result.TournamentLocation.SecondaryAddressableObjectName);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentLocation.PrimaryAddressableObjectName, result.TournamentLocation.PrimaryAddressableObjectName);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentLocation.StreetDescription, result.TournamentLocation.StreetDescription);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentLocation.Locality, result.TournamentLocation.Locality);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentLocation.Town, result.TournamentLocation.Town);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentLocation.AdministrativeArea, result.TournamentLocation.AdministrativeArea);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentLocation.Postcode, result.TournamentLocation.Postcode);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentLocation.Latitude, result.TournamentLocation.Latitude);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentLocation.Longitude, result.TournamentLocation.Longitude);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentLocation.GeoPrecision, result.TournamentLocation.GeoPrecision);
            Assert.Equal(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentLocation.MatchLocationNotes, result.TournamentLocation.MatchLocationNotes);
        }

        [Fact]
        public async Task Read_tournament_by_route_returns_season_with_competition()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.TournamentInThePastWithFullDetails!.TournamentRoute, "tournaments")).Returns(_databaseFixture.TestData.TournamentInThePastWithFullDetails!.TournamentRoute);
            var tournamentDataSource = new SqlServerTournamentDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await tournamentDataSource.ReadTournamentByRoute(_databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentRoute).ConfigureAwait(false);
            Assert.NotNull(result);

            foreach (var season in _databaseFixture.TestData.TournamentInThePastWithFullDetails.Seasons)
            {
                var resultSeason = result!.Seasons.SingleOrDefault(x => x.SeasonId == season.SeasonId);
                Assert.NotNull(resultSeason);
                Assert.Equal(season.SeasonRoute, resultSeason!.SeasonRoute);
                Assert.Equal(season.FromYear, resultSeason.FromYear);
                Assert.Equal(season.UntilYear, resultSeason.UntilYear);
                Assert.Equal(season.Competition.CompetitionName, resultSeason.Competition.CompetitionName);
            }
        }
    }
}
