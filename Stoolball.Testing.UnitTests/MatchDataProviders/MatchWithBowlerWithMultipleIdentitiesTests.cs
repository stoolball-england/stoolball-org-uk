using System.Linq;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Testing.Factories;
using Stoolball.Testing.MatchDataProviders;
using Xunit;

namespace Stoolball.Testing.UnitTests.MatchDataProviders
{
    public class MatchWithBowlerWithMultipleIdentitiesTests(TestServicesFixture _services) : IClassFixture<TestServicesFixture>
    {
        [Fact]
        public void A_player_linked_to_a_member_bowls_using_two_identities()
        {
            var provider = new MatchWithBowlerWithMultipleIdentities(
                _services.Get<MatchFactory>(), _services.Get<TeamFactory>(), _services.Get<PlayerFactory>());

            var match = provider.CreateMatches(new TestData()).Single();

            var bowlerIdentities = match.MatchInnings[0].PlayerInnings.Select(x => x.Bowler).OfType<PlayerIdentity>().Distinct(new PlayerIdentityEqualityComparer()).ToList();

            Assert.Equal(2, bowlerIdentities.Count);
            Assert.Equal(bowlerIdentities[0].Player!.PlayerId, bowlerIdentities[1].Player!.PlayerId);
            Assert.Equal(2, bowlerIdentities[0].Player!.PlayerIdentities.Count);
            Assert.NotNull(bowlerIdentities[0].Player!.MemberKey);
            Assert.All(bowlerIdentities, identity => Assert.Equal(PlayerIdentityLinkedBy.Member, identity.LinkedBy));
        }
    }
}
