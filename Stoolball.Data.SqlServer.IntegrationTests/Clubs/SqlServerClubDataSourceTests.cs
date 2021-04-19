using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Clubs;
using Stoolball.Routing;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Clubs
{
    [Collection(IntegrationTestConstants.DataSourceIntegrationTestCollection)]
    public class SqlServerClubDataSourceTests
    {
        private readonly SqlServerDataSourceFixture _databaseFixture;

        public SqlServerClubDataSourceTests(SqlServerDataSourceFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }
        private static void AssertTeamsSortedAlphabeticallyWithInactiveLast(IEnumerable<Team> teams)
        {
            var expectedActiveStatus = true;
            var previousName = string.Empty;
            foreach (var team in teams)
            {
                // The first time an inactive team is seen, set a flag to say they must all be inactive
                if (expectedActiveStatus && team.UntilYear.HasValue)
                {
                    expectedActiveStatus = false;
                    previousName = string.Empty;
                }

                Assert.True(StringSortOrderIs(previousName, team.ComparableName()));
                previousName = team.ComparableName();

                Assert.Equal(expectedActiveStatus, !team.UntilYear.HasValue);
            }
            Assert.False(expectedActiveStatus);
        }

        private static bool StringSortOrderIs(string shouldBeFirst, string shouldBeSecond)
        {
            var result = new[] { shouldBeFirst, shouldBeSecond };
            Array.Sort(result);

            return (result[0] == shouldBeFirst && result[1] == shouldBeSecond);
        }

        [Fact]
        public async Task Read_minimal_club_by_route_returns_basic_club_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.ClubWithMinimalDetails.ClubRoute, "clubs")).Returns(_databaseFixture.ClubWithMinimalDetails.ClubRoute);
            var clubDataSource = new SqlServerClubDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await clubDataSource.ReadClubByRoute(_databaseFixture.ClubWithMinimalDetails.ClubRoute).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.ClubWithMinimalDetails.ClubId, result.ClubId);
            Assert.Equal(_databaseFixture.ClubWithMinimalDetails.ClubName, result.ClubName);
            Assert.Equal(_databaseFixture.ClubWithMinimalDetails.ClubRoute, result.ClubRoute);
            Assert.Equal(_databaseFixture.ClubWithMinimalDetails.MemberGroupKey, result.MemberGroupKey);
            Assert.Equal(_databaseFixture.ClubWithMinimalDetails.MemberGroupName, result.MemberGroupName);
        }

        [Fact]
        public async Task Read_club_by_route_returns_teams_alphabetically_with_inactive_last()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.ClubWithTeamsAndMatchLocation.ClubRoute, "clubs")).Returns(_databaseFixture.ClubWithTeamsAndMatchLocation.ClubRoute);
            var clubDataSource = new SqlServerClubDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await clubDataSource.ReadClubByRoute(_databaseFixture.ClubWithTeamsAndMatchLocation.ClubRoute).ConfigureAwait(false);

            AssertTeamsSortedAlphabeticallyWithInactiveLast(result.Teams);
        }

        [Fact]
        public async Task Read_clubs_returns_basic_club_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var clubDataSource = new SqlServerClubDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await clubDataSource.ReadClubs(null).ConfigureAwait(false);

            foreach (var club in _databaseFixture.Clubs)
            {
                var result = results.SingleOrDefault(x => x.ClubId == club.ClubId);

                Assert.NotNull(result);
                Assert.Equal(club.ClubName, result.ClubName);
                Assert.Equal(club.ClubRoute, result.ClubRoute);
                Assert.Equal(club.MemberGroupKey, result.MemberGroupKey);
                Assert.Equal(club.MemberGroupName, result.MemberGroupName);
            }
        }

        [Fact]
        public async Task Read_clubs_returns_teams()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var clubDataSource = new SqlServerClubDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await clubDataSource.ReadClubs(null).ConfigureAwait(false);

            foreach (var club in _databaseFixture.Clubs)
            {
                var resultClub = results.SingleOrDefault(x => x.ClubId == club.ClubId);

                foreach (var team in club.Teams)
                {
                    var resultTeam = resultClub.Teams.FirstOrDefault(x => x.TeamId == team.TeamId);
                    Assert.NotNull(resultTeam);

                    Assert.Equal(team.TeamName, resultTeam.TeamName);
                    Assert.Equal(team.TeamRoute, resultTeam.TeamRoute);
                    Assert.Equal(team.ClubMark, resultTeam.ClubMark);
                    Assert.Equal(team.UntilYear, resultTeam.UntilYear);
                }
            }
        }

        [Fact]
        public async Task Read_clubs_sorts_teams_alphabetically_with_inactive_last()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var clubDataSource = new SqlServerClubDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await clubDataSource.ReadClubs(null).ConfigureAwait(false);

            foreach (var club in results.Where(x => x.Teams.Count > 1))
            {
                AssertTeamsSortedAlphabeticallyWithInactiveLast(club.Teams);
            }
        }

        [Fact]
        public async Task Read_clubs_supports_no_filter()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var clubDataSource = new SqlServerClubDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await clubDataSource.ReadClubs(null).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.Clubs.Count, results.Count);
            foreach (var club in _databaseFixture.Clubs)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.ClubId == club.ClubId));
            }
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Assertions", "xUnit2012:Do not use Enumerable.Any() to check if a value exists in a collection", Justification = "Not checking for a Team equal by reference, but for another team with the same id")]
        public async Task Read_clubs_supports_filter_by_team_id()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var clubDataSource = new SqlServerClubDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await clubDataSource.ReadClubs(new ClubFilter { TeamIds = new List<Guid> { _databaseFixture.ClubWithTeamsAndMatchLocation.Teams[0].TeamId.Value } }).ConfigureAwait(false);

            Assert.Single(result);
            Assert.True(result[0].Teams.Any(x => x.TeamId == _databaseFixture.ClubWithTeamsAndMatchLocation.Teams[0].TeamId));
        }
    }
}
