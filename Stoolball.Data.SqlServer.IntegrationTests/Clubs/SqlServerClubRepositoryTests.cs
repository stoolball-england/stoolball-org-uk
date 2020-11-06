using System;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Moq;
using Stoolball.Clubs;
using Stoolball.Logging;
using Stoolball.Routing;
using Xunit;
using static Stoolball.Data.SqlServer.Constants;

namespace Stoolball.Data.SqlServer.IntegrationTests.Clubs
{
    public class SqlServerClubRepositoryTests : IDisposable
    {
        private TransactionScope _testScope;
        private IDatabaseConnectionFactory _connectionFactory;

        public SqlServerClubRepositoryTests()
        {
            _testScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            _connectionFactory = new IntegrationTestsDatabaseConnectionFactory();
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

            var repo = new SqlServerClubRepository(_connectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger>(), routeGenerator.Object, Mock.Of<IRedirectsRepository>());
            var memberKey = Guid.NewGuid();
            var memberName = "Person 1";

            var createdClub = await repo.CreateClub(club, memberKey, memberName);

            using (var connection = _connectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Club>($"SELECT ClubId FROM {Tables.Club} WHERE ClubId = @ClubId", new { createdClub.ClubId }).ConfigureAwait(false);
                Assert.NotNull(result);
            }
        }

        public void Dispose()
        {
            _testScope.Dispose();
        }
    }
}
