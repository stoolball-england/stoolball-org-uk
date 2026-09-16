using System.Linq;
using Stoolball.Testing.ClubDataProviders;
using Stoolball.Testing.Factories;
using Xunit;

namespace Stoolball.Testing.UnitTests.ClubDataProviders
{
    public class ClubWithTeamsAndMatchLocationProviderTests(TestServicesFixture _services) : IClassFixture<TestServicesFixture>
    {
        [Fact]
        public void Club_has_teams_and_an_active_team_has_a_match_location()
        {
            var provider = new ClubWithTeamsAndMatchLocationProvider(_services.Get<ClubFactory>(), _services.Get<MatchLocationFactory>());

            var club = provider.CreateClubs(new TestData()).Single();

            Assert.Equal(3, club.Teams.Count);
            var teamWithMatchLocation = Assert.Single(club.Teams, x => x.MatchLocations.Any());
            Assert.False(teamWithMatchLocation.UntilYear.HasValue);

            var matchLocation = teamWithMatchLocation.MatchLocations.Single();
            Assert.Contains(teamWithMatchLocation, matchLocation.Teams);
        }
    }
}
