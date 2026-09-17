using System.Linq;
using Stoolball.Teams;
using Stoolball.Testing.Factories;
using Xunit;

namespace Stoolball.Testing.UnitTests.Factories
{
    public class MatchLocationFactoryTests(TestServicesFixture _services) : IClassFixture<TestServicesFixture>
    {
        [Fact]
        public void Match_location_with_full_details_has_active_transient_and_inactive_teams()
        {
            var matchLocation = _services.Get<MatchLocationFactory>().CreateMatchLocationWithFullDetails(_services.Get<TeamFactory>().CreateBasicTeamFaker());

            Assert.Equal(4, matchLocation.Teams.Count);
            Assert.Contains(matchLocation.Teams, x => x.TeamType == TeamType.Transient);
            Assert.Contains(matchLocation.Teams, x => x.UntilYear.HasValue);
            Assert.Equal(2, matchLocation.Teams.Count(x => !x.UntilYear.HasValue && x.TeamType != TeamType.Transient));
            Assert.All(matchLocation.Teams, x => Assert.Contains(matchLocation, x.MatchLocations));
        }
    }
}
