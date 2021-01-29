using System;
using System.Threading.Tasks;
using Dapper;
using Moq;
using Stoolball.Clubs;
using Stoolball.Logging;
using Stoolball.Routing;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.IntegrationTests.Clubs
{
    [Collection(IntegrationTestConstants.IntegrationTestCollection)]
    public class SqlServerClubRepositoryTests
    {
        private readonly DatabaseFixture _databaseFixture;

        public SqlServerClubRepositoryTests(DatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Create_club_succeeds()
        {
            var club = new Club
            {
                ClubName = "New club " + Guid.NewGuid(),
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Test group"
            };

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateRoute("/clubs", club.ClubName, NoiseWords.ClubRoute)).Returns("/clubs/" + Guid.NewGuid());

            var repo = new SqlServerClubRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger>(), routeGenerator.Object, Mock.Of<IRedirectsRepository>());
            var memberKey = Guid.NewGuid();
            var memberName = "Person 1";

            var createdClub = await repo.CreateClub(club, memberKey, memberName).ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Club>($"SELECT ClubId FROM {Tables.Club} WHERE ClubId = @ClubId", new { createdClub.ClubId }).ConfigureAwait(false);
                Assert.NotNull(result);
            }
        }
    }
}
