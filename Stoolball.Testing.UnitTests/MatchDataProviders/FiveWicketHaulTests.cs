using System;
using System.Linq;
using Stoolball.Awards;
using Stoolball.Statistics;
using Stoolball.Testing;
using Stoolball.Testing.Factories;
using Stoolball.Testing.MatchDataProviders;
using Xunit;

namespace Stoolball.Testing.UnitTests.MatchDataProviders
{
    public class FiveWicketHaulTests
    {
        [Fact]
        public void Five_wicket_haul_exists()
        {
            var randomiser = new Randomiser(new Random());
            var overSetFactory = new OverSetFactory();
            var matchFactory = new MatchFactory(randomiser, new Award { AwardId = Guid.NewGuid(), AwardName = "Player of the match" }, overSetFactory);
            var provider = new FiveWicketHaul(matchFactory, new TeamFactory(), new PlayerFactory());

            var innings = provider.CreateMatches(new TestData()).SelectMany(x => x.MatchInnings);

            var inningsWithFiveWicketHaulExists = innings.Any(x => // return true for this MatchInnings if...
                        x.PlayerInnings.Where(pi => StatisticsConstants.DISMISSALS_CREDITED_TO_BOWLER.Contains(pi.DismissalType) && pi.Bowler != null) // for all wickets credited to a bowler...
                        .GroupBy(pi => pi.Bowler!.Player!.PlayerId) // when grouped by bowler...
                        .Any(dismissals => dismissals.Count() >= 5)); // any bowler has 5 or more

            Assert.True(inningsWithFiveWicketHaulExists);
        }
    }
}
