using System;
using System.Linq;
using Stoolball.Awards;
using Stoolball.Matches;
using Stoolball.Testing;
using Stoolball.Testing.Factories;
using Stoolball.Testing.MatchDataProviders;
using Xunit;

namespace Stoolball.Testing.UnitTests.MatchDataProviders
{
    public class FieldingByMultipleIdentitiesTests
    {
        [Fact]
        public void A_catcher_and_a_run_out_fielder_each_have_two_identities_credited()
        {
            var randomiser = new Randomiser(new Random());
            var overSetFactory = new OverSetFactory();
            var matchFactory = new MatchFactory(randomiser, new Award { AwardId = Guid.NewGuid(), AwardName = "Player of the match" }, overSetFactory);
            var teamFactory = new TeamFactory(new CompetitionFactory(new SeasonFactory(overSetFactory)), new SeasonFactory(overSetFactory), new MatchLocationFactory());
            var provider = new FieldingByMultipleIdentities(randomiser, matchFactory, teamFactory, new PlayerFactory());

            var match = provider.CreateMatches(new TestData()).Single();

            // The provider only overrides the first six player innings in each of these innings - later ones are random and may be unrelated dismissals.
            var catchesAndCaughtAndBowled = match.MatchInnings[0].PlayerInnings.Take(6).ToList();
            Assert.All(catchesAndCaughtAndBowled, pi => Assert.True(pi.DismissalType == DismissalType.Caught || pi.DismissalType == DismissalType.CaughtAndBowled));
            Assert.Contains(catchesAndCaughtAndBowled, pi => pi.DismissalType == DismissalType.Caught);
            Assert.Contains(catchesAndCaughtAndBowled, pi => pi.DismissalType == DismissalType.CaughtAndBowled);

            var catcherIdentityIds = catchesAndCaughtAndBowled
                .Select(pi => pi.DismissalType == DismissalType.Caught ? pi.DismissedBy!.PlayerIdentityId : pi.Bowler!.PlayerIdentityId)
                .Distinct();
            var catcherPlayerIds = catchesAndCaughtAndBowled
                .Select(pi => pi.DismissalType == DismissalType.Caught ? pi.DismissedBy!.Player!.PlayerId : pi.Bowler!.Player!.PlayerId)
                .Distinct();

            Assert.Equal(2, catcherIdentityIds.Count());
            Assert.Single(catcherPlayerIds);

            var runOuts = match.MatchInnings[1].PlayerInnings.Take(6).ToList();
            Assert.All(runOuts, pi => Assert.Equal(DismissalType.RunOut, pi.DismissalType));

            var fielderIdentityIds = runOuts.Select(pi => pi.DismissedBy!.PlayerIdentityId).Distinct();
            var fielderPlayerIds = runOuts.Select(pi => pi.DismissedBy!.Player!.PlayerId).Distinct();

            Assert.Equal(2, fielderIdentityIds.Count());
            Assert.Single(fielderPlayerIds);
        }
    }
}
