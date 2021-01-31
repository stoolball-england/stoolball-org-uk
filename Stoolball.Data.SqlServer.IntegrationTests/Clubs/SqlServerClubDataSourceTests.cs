using System;
using System.Threading.Tasks;
using Moq;
using Stoolball.Routing;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Clubs
{
    [Collection(IntegrationTestConstants.IntegrationTestCollection)]
    public class SqlServerClubDataSourceTests
    {
        private readonly DatabaseFixture _databaseFixture;

        public SqlServerClubDataSourceTests(DatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_minimal_club_by_route_succeeds()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.ClubWithMinimalDetails.ClubRoute, "clubs")).Returns(_databaseFixture.ClubWithMinimalDetails.ClubRoute);
            var clubDataSource = new SqlServerClubDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await clubDataSource.ReadClubByRoute(_databaseFixture.ClubWithMinimalDetails.ClubRoute).ConfigureAwait(false);

            Assert.NotNull(result);
        }
    }
}
