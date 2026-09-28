using System.Linq;
using Stoolball.Testing.ClubDataProviders;
using Stoolball.Testing.Factories;
using Xunit;

namespace Stoolball.Testing.UnitTests.ClubDataProviders
{
    public class ClubWithMinimalDetailsProviderTests(TestServicesFixture _services) : IClassFixture<TestServicesFixture>
    {
        [Fact]
        public void Club_has_no_teams()
        {
            var provider = new ClubWithMinimalDetailsProvider(_services.Get<ClubFactory>());

            var club = provider.CreateClubs(new TestData()).Single();

            Assert.Empty(club.Teams);
        }
    }
}
