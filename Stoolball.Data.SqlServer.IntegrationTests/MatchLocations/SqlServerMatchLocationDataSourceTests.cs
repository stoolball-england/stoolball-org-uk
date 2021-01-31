using System;
using System.Threading.Tasks;
using Moq;
using Stoolball.Routing;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.MatchLocations
{
    [Collection(IntegrationTestConstants.IntegrationTestCollection)]
    public class SqlServerMatchLocationDataSourceTests
    {
        private readonly DatabaseFixture _databaseFixture;

        public SqlServerMatchLocationDataSourceTests(DatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_minimal_location_by_route_succeeds()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchLocationWithMinimalDetails.MatchLocationRoute, "locations")).Returns(_databaseFixture.MatchLocationWithMinimalDetails.MatchLocationRoute);
            var locationDataSource = new SqlServerMatchLocationDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await locationDataSource.ReadMatchLocationByRoute(_databaseFixture.MatchLocationWithMinimalDetails.MatchLocationRoute, false).ConfigureAwait(false);

            Assert.NotNull(result);
        }
    }
}
