using System;
using System.Threading.Tasks;
using Moq;
using Stoolball.Routing;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Clubs
{
    [Collection(IntegrationTestConstants.DataSourceIntegrationTestCollection)]
    public class SqlServerClubDataSourceTests
    {
        private readonly SqlServerDataSourceFixture _databaseFixture;

        public SqlServerClubDataSourceTests(SqlServerDataSourceFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_minimal_club_by_route_returns_basic_club_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.ClubWithMinimalDetails.ClubRoute, "clubs")).Returns(_databaseFixture.ClubWithMinimalDetails.ClubRoute);
            var clubDataSource = new SqlServerClubDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await clubDataSource.ReadClubByRoute(_databaseFixture.ClubWithMinimalDetails.ClubRoute).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.ClubWithMinimalDetails.ClubId, result.ClubId);
            Assert.Equal(_databaseFixture.ClubWithMinimalDetails.ClubName, result.ClubName);
            Assert.Equal(_databaseFixture.ClubWithMinimalDetails.ClubRoute, result.ClubRoute);
            Assert.Equal(_databaseFixture.ClubWithMinimalDetails.MemberGroupKey, result.MemberGroupKey);
            Assert.Equal(_databaseFixture.ClubWithMinimalDetails.MemberGroupName, result.MemberGroupName);
        }

        [Fact]
        public async Task Read_club_by_route_returns_teams_alphabetically_with_inactive_last()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.ClubWithTeams.ClubRoute, "clubs")).Returns(_databaseFixture.ClubWithTeams.ClubRoute);
            var clubDataSource = new SqlServerClubDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await clubDataSource.ReadClubByRoute(_databaseFixture.ClubWithTeams.ClubRoute).ConfigureAwait(false);

            for (var team = 0; team < _databaseFixture.ClubWithTeams.Teams.Count; team++)
            {
                Assert.Equal(_databaseFixture.ClubWithTeams.Teams[team].TeamId, result.Teams[team].TeamId);
                Assert.Equal(_databaseFixture.ClubWithTeams.Teams[team].TeamName, result.Teams[team].TeamName);
                Assert.Equal(_databaseFixture.ClubWithTeams.Teams[team].TeamRoute, result.Teams[team].TeamRoute);
                Assert.Equal(_databaseFixture.ClubWithTeams.Teams[team].ClubMark, result.Teams[team].ClubMark);
                Assert.Equal(_databaseFixture.ClubWithTeams.Teams[team].UntilYear, result.Teams[team].UntilYear);
            }
        }
    }
}
