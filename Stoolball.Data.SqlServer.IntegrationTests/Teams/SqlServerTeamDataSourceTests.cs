using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Stoolball.Routing;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Teams
{
    [Collection(IntegrationTestConstants.IntegrationTestCollection)]
    public class SqlServerTeamDataSourceTests
    {
        private readonly DatabaseFixture _databaseFixture;

        public SqlServerTeamDataSourceTests(DatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_minimal_team_by_route_succeeds()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TeamWithMinimalDetails.TeamRoute, It.IsAny<Dictionary<string, string>>())).Returns(_databaseFixture.TeamWithMinimalDetails.TeamRoute);
            var teamDataSource = new SqlServerTeamDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await teamDataSource.ReadTeamByRoute(_databaseFixture.TeamWithMinimalDetails.TeamRoute, false).ConfigureAwait(false);

            Assert.NotNull(result);
        }
    }
}
