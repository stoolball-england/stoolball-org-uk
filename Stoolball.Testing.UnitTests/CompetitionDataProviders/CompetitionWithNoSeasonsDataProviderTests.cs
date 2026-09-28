using System.Linq;
using Stoolball.Testing.CompetitionDataProviders;
using Stoolball.Testing.Factories;
using Xunit;

namespace Stoolball.Testing.UnitTests.CompetitionDataProviders
{
    public class CompetitionWithNoSeasonsDataProviderTests(TestServicesFixture _services) : IClassFixture<TestServicesFixture>
    {
        [Fact]
        public void Competition_has_no_seasons()
        {
            var provider = new CompetitionWithNoSeasonsDataProvider(_services.Get<CompetitionFactory>());

            var competition = provider.CreateCompetitions(new TestData()).Single();

            Assert.Empty(competition.Seasons);
        }
    }
}
