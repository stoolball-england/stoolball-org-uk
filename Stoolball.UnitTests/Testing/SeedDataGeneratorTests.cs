using System.Linq;
using Moq;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Testing;
using Stoolball.Testing.Fakers;
using Xunit;

namespace Stoolball.UnitTests.Testing
{
    public class SeedDataGeneratorTests
    {
        // Run each test enough times to be confident random data generation always matches the test
        private const int _iterations = 10;

        [Fact]
        public void Over_exists_with_only_a_bowler_name()
        {
            var generator = new SeedDataGenerator(Mock.Of<IOversHelper>(), Mock.Of<IBowlingFiguresCalculator>(), Mock.Of<IPlayerIdentityFinder>(),
                Mock.Of<TeamFakerFactory>(), Mock.Of<MatchLocationFakerFactory>(), Mock.Of<SchoolFakerFactory>());

            for (var i = 0; i < _iterations; i++)
            {
                var overs = generator.CreateOversBowled(generator.GenerateTeams()[0].identities, generator.CreateOverSets());

                Assert.Contains(overs, x => x.Bowler != null && x.BallsBowled == null && x.NoBalls == null && x.Wides == null && x.RunsConceded == null);
            }
        }

        [Fact]
        public void Five_wicket_haul_exists()
        {
            var generator = new SeedDataGenerator(Mock.Of<IOversHelper>(), Mock.Of<IBowlingFiguresCalculator>(), Mock.Of<IPlayerIdentityFinder>(),
                Mock.Of<TeamFakerFactory>(), Mock.Of<MatchLocationFakerFactory>(), Mock.Of<SchoolFakerFactory>());

            for (var i = 0; i < _iterations; i++)
            {
                var innings = generator.GenerateMatchData().SelectMany(x => x.MatchInnings);

                var inningsWithFiveWicketHaulExists = innings.Any(x => // return true for this MatchInnings if...
                            x.PlayerInnings.Where(pi => StatisticsConstants.DISMISSALS_CREDITED_TO_BOWLER.Contains(pi.DismissalType) && pi.Bowler != null) // for all wickets credited to a bowler...
                            .GroupBy(pi => pi.Bowler.Player.PlayerId.Value) // when grouped by bowler...
                            .Any(dismissals => dismissals.Count() >= 5)); // any bowler has 5 or more

                Assert.True(inningsWithFiveWicketHaulExists);
            }
        }
    }
}
