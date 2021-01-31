using System;
using System.Threading.Tasks;
using Moq;
using Stoolball.Routing;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Competitions
{
    [Collection(IntegrationTestConstants.IntegrationTestCollection)]
    public class SqlServerCompetitionDataSourceTests
    {
        private readonly DatabaseFixture _databaseFixture;

        public SqlServerCompetitionDataSourceTests(DatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_minimal_competition_by_route_succeeds()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.CompetitionWithMinimalDetails.CompetitionRoute, "competitions")).Returns(_databaseFixture.CompetitionWithMinimalDetails.CompetitionRoute);
            var competitionDataSource = new SqlServerCompetitionDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await competitionDataSource.ReadCompetitionByRoute(_databaseFixture.CompetitionWithMinimalDetails.CompetitionRoute).ConfigureAwait(false);

            Assert.NotNull(result);
        }
    }
}
