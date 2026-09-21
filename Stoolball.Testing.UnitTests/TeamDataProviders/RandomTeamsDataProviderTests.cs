using System.Linq;
using Stoolball.Testing.Factories;
using Stoolball.Testing.TeamDataProviders;
using Xunit;

namespace Stoolball.Testing.UnitTests.TeamDataProviders
{
    public class RandomTeamsDataProviderTests(TestServicesFixture _services) : IClassFixture<TestServicesFixture>
    {
        private RandomTeamsDataProvider CreateProvider() =>
            new(_services.Get<Randomiser>(), _services.Get<TeamFactory>(), _services.Get<PlayerFactory>());

        [Fact]
        public void Five_teams_are_created_with_eight_identities_each()
        {
            var teams = CreateProvider().CreateTeams(new TestData()).ToList();

            Assert.Equal(5, teams.Count);
            Assert.All(teams, x => Assert.Equal(8, x.identities.Count));
            Assert.All(teams, x => Assert.All(x.identities, i => Assert.Equal(x.team.TeamId, i.Team!.TeamId)));
        }

        [Fact]
        public void Some_players_have_more_than_one_identity()
        {
            var identities = CreateProvider().CreateTeams(new TestData()).SelectMany(x => x.identities).ToList();

            Assert.Contains(identities, i => identities.Count(x => x.Player!.PlayerId == i.Player!.PlayerId) > 1);
        }
    }
}
