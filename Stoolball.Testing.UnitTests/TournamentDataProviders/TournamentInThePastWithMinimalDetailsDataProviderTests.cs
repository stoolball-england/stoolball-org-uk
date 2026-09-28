using System;
using System.Linq;
using Stoolball.Testing.Factories;
using Stoolball.Testing.TournamentDataProviders;
using Xunit;

namespace Stoolball.Testing.UnitTests.TournamentDataProviders
{
    public class TournamentInThePastWithMinimalDetailsDataProviderTests(TestServicesFixture _services) : IClassFixture<TestServicesFixture>
    {
        [Fact]
        public void Tournament_is_in_the_past_and_has_minimal_details()
        {
            var provider = new TournamentInThePastWithMinimalDetailsDataProvider(_services.Get<TournamentFactory>());

            var (tournament, matches) = provider.CreateTournaments(new TestData()).Single();

            Assert.True(tournament.StartTime < DateTimeOffset.UtcNow);
            Assert.Empty(tournament.Teams);
            Assert.Empty(tournament.Seasons);
            Assert.Empty(tournament.History);
            Assert.Empty(tournament.Comments);
            Assert.Null(tournament.TournamentLocation);
            Assert.Empty(matches);
        }
    }
}
