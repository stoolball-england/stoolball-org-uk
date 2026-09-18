using System.Linq;
using Stoolball.Testing.Factories;
using Stoolball.Testing.MatchDataProviders;
using Xunit;

namespace Stoolball.Testing.UnitTests.MatchDataProviders
{
    public class MatchInThePastWithNoTeamsDataProviderTests(TestServicesFixture _services) : IClassFixture<TestServicesFixture>
    {
        [Fact]
        public void Match_is_in_the_past_and_has_no_teams()
        {
            var provider = new MatchInThePastWithNoTeamsDataProvider(_services.Get<MatchFactory>());

            var match = provider.CreateMatches(new TestData()).Single();

            Assert.Empty(match.Teams);
            Assert.True(match.StartTime < System.DateTimeOffset.UtcNow);
        }
    }
}
