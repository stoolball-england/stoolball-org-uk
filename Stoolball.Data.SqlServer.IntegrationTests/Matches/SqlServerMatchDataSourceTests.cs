using System;
using System.Threading.Tasks;
using Moq;
using Stoolball.Routing;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches
{
    [Collection(IntegrationTestConstants.IntegrationTestCollection)]
    public class SqlServerMatchDataSourceTests
    {
        private readonly DatabaseFixture _databaseFixture;

        public SqlServerMatchDataSourceTests(DatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_minimal_match_by_route_succeeds()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchInThePastWithMinimalDetails.MatchRoute, "matches")).Returns(_databaseFixture.MatchInThePastWithMinimalDetails.MatchRoute);
            var matchDataSource = new SqlServerMatchDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchDataSource.ReadMatchByRoute(_databaseFixture.MatchInThePastWithMinimalDetails.MatchRoute).ConfigureAwait(false);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task Read_full_match_by_route_succeeds()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute, "matches")).Returns(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute);
            var matchDataSource = new SqlServerMatchDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchDataSource.ReadMatchByRoute(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute).ConfigureAwait(false);

            Assert.NotNull(result);
        }
    }
}
