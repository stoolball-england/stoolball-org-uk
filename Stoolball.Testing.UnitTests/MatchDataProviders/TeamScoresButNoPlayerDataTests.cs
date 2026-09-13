using System;
using System.Linq;
using Stoolball.Awards;
using Stoolball.Testing.Factories;
using Stoolball.Testing.MatchDataProviders;
using Xunit;

namespace Stoolball.Testing.UnitTests.MatchDataProviders
{
    public class TeamScoresButNoPlayerDataTests
    {
        [Fact]
        public void Match_has_team_scores_but_no_player_data()
        {
            var randomiser = new Randomiser(new Random());
            var overSetFactory = new OverSetFactory();
            var matchFactory = new MatchFactory(randomiser, new Award { AwardId = Guid.NewGuid(), AwardName = "Player of the match" }, overSetFactory);
            var provider = new TeamScoresButNoPlayerData(randomiser, matchFactory, new TeamFactory());

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
