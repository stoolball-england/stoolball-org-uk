using System.Linq;
using Stoolball.Awards;
using Stoolball.Statistics;
using Stoolball.Testing.Factories;
using Stoolball.Testing.MatchDataProviders;
using Xunit;

namespace Stoolball.Testing.UnitTests.MatchDataProviders
{
    public class APlayerOnlyWinsAnAwardButHasPlayedOtherMatchesWithADifferentTeamTests(TestServicesFixture _services) : IClassFixture<TestServicesFixture>
    {
        private APlayerOnlyWinsAnAwardButHasPlayedOtherMatchesWithADifferentTeam CreateProvider() =>
            new(_services.Get<Randomiser>(), _services.Get<MatchFactory>(), _services.Get<Award>(),
                _services.Get<TeamFactory>(), _services.Get<PlayerFactory>());

        [Fact]
        public void Creates_matches_without_needing_any_teams_in_the_test_data()
        {
            var matches = CreateProvider().CreateMatches(new TestData()).ToList();

            Assert.Equal(2, matches.Count);
        }

        [Fact]
        public void Player_only_wins_an_award_in_a_match_against_the_team_they_play_for()
        {
            var matches = CreateProvider().CreateMatches(new TestData()).ToList();

            var identityOnTeamPlayedFor = matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings)
                .Select(x => x.Bowler).OfType<PlayerIdentity>()
                .First();
            var award = matches.SelectMany(x => x.Awards).Single();

            Assert.Equal(identityOnTeamPlayedFor.Player!.PlayerId, award.PlayerIdentity!.Player!.PlayerId);
            Assert.NotEqual(identityOnTeamPlayedFor.Team!.TeamId, award.PlayerIdentity.Team!.TeamId);
            Assert.Contains(matches.Single(x => x.Awards.Any()).Teams, t => t.Team!.TeamId == identityOnTeamPlayedFor.Team.TeamId);
        }
    }
}
