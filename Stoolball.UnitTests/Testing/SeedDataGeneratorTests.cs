using Moq;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Testing;
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
            var generator = new SeedDataGenerator(Mock.Of<IOversHelper>(), Mock.Of<IBowlingFiguresCalculator>(), Mock.Of<IPlayerIdentityFinder>());

            for (var i = 0; i < _iterations; i++)
            {
                var overs = generator.CreateOversBowled(generator.GenerateTeams()[0].identities, generator.CreateOverSets());

                Assert.Contains(overs, x => x.Bowler != null && x.BallsBowled == null && x.NoBalls == null && x.Wides == null && x.RunsConceded == null);
            }
        }


    }
}
