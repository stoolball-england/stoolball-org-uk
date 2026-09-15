using System.Linq;
using Stoolball.Testing.Factories;
using Stoolball.Testing.MatchDataProviders;
using Xunit;

namespace Stoolball.Testing.UnitTests.MatchDataProviders
{
    public class TeamScoresButNoPlayerDataTests(TestServicesFixture _services) : IClassFixture<TestServicesFixture>
    {
        [Fact]
        public void Match_has_team_scores_but_no_player_data()
        {
            var provider = new TeamScoresButNoPlayerData(_services.Get<Randomiser>(), _services.Get<MatchFactory>(), _services.Get<TeamFactory>());

            var match = provider.CreateMatches(new TestData()).Single();

            Assert.NotNull(match.MatchInnings[0].Runs);
            Assert.True(match.MatchInnings[0].Runs > 0);
            Assert.NotNull(match.MatchInnings[0].Wickets);
            Assert.True(match.MatchInnings[0].Wickets > 0);
            Assert.NotNull(match.MatchInnings[1].Runs);
            Assert.True(match.MatchInnings[1].Runs > 0);
            Assert.NotNull(match.MatchInnings[1].Wickets);
            Assert.True(match.MatchInnings[1].Wickets > 0);

            foreach (var innings in match.MatchInnings)
            {
                Assert.Empty(innings.PlayerInnings);
                Assert.Empty(innings.OversBowled);
            }
            Assert.Empty(match.Awards);
        }
    }
}
