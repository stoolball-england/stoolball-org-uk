using System;
using System.Linq;
using Stoolball.Awards;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Testing;
using Stoolball.Testing.Factories;
using Stoolball.Testing.MatchDataProviders;
using Xunit;

namespace Stoolball.Testing.UnitTests.MatchDataProviders
{
    public class PlentyOfRunOutsAndCatchesTests
    {
        [Fact]
        public void At_least_six_distinct_players_have_a_run_out_and_at_least_six_distinct_players_have_a_catch()
        {
            var randomiser = new Randomiser(new Random());
            var overSetFactory = new OverSetFactory();
            var matchFactory = new MatchFactory(randomiser, new Award { AwardId = Guid.NewGuid(), AwardName = "Player of the match" }, overSetFactory);
            var provider = new PlentyOfRunOutsAndCatches(matchFactory, new TeamFactory(), new PlayerFactory());

            var playerInnings = provider.CreateMatches(new TestData()).SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings);

            var playersWithARunOut = playerInnings
                .Where(pi => pi.DismissalType == DismissalType.RunOut && pi.DismissedBy != null)
                .Select(pi => pi.DismissedBy!.Player!.PlayerId)
                .Distinct();

            var playersWithACatch = playerInnings
                .Where(pi => pi.DismissalType == DismissalType.Caught && pi.DismissedBy != null)
                .Select(pi => pi.DismissedBy!.Player!.PlayerId)
                .Distinct();

            Assert.True(playersWithARunOut.Count() >= 6);
            Assert.True(playersWithACatch.Count() >= 6);
        }
    }
}
