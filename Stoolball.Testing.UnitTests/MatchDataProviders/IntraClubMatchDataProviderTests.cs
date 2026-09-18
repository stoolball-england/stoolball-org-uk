using System.Linq;
using Stoolball.Testing.Factories;
using Stoolball.Testing.MatchDataProviders;
using Xunit;

namespace Stoolball.Testing.UnitTests.MatchDataProviders
{
    public class IntraClubMatchDataProviderTests(TestServicesFixture _services) : IClassFixture<TestServicesFixture>
    {
        [Fact]
        public void The_same_team_is_on_both_sides_of_the_match()
        {
            var provider = new IntraClubMatchDataProvider(_services.Get<Randomiser>(), _services.Get<MatchFactory>(), _services.Get<TeamFactory>(), _services.Get<PlayerFactory>());

            var match = provider.CreateMatches(new TestData()).Single();

            Assert.Equal(2, match.Teams.Count);
            Assert.Equal(match.Teams[0].Team!.TeamId, match.Teams[1].Team!.TeamId);
            Assert.NotEqual(match.Teams[0].MatchTeamId, match.Teams[1].MatchTeamId);
        }
    }
}
