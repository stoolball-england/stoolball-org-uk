using System;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Redirects
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SkybrudRedirectsRepositoryTests : IDisposable
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly TransactionScope _scope;

        public SkybrudRedirectsRepositoryTests(SqlServerTestDataFixture databaseFixture)
        {
            _connectionFactory = databaseFixture?.ConnectionFactory ?? throw new ArgumentException($"{nameof(databaseFixture)}.{nameof(databaseFixture.ConnectionFactory)} cannot be null", nameof(databaseFixture));
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        private SkybrudRedirectsRepository CreateRepository()
        {
            return new SkybrudRedirectsRepository();
        }

#nullable disable
        [Theory]
        [InlineData("/teams/team-a", "/teams/team-b", null)]
        [InlineData("/teams/team-c", "/teams/team-d", "/players")]
        public async Task Create_redirect_works(string original, string revised, string? suffix)
        {
            var repo = CreateRepository();

            using (var connection = _connectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await repo.InsertRedirect(original, revised, suffix, transaction).ConfigureAwait(false);
                    transaction.Commit();
                }

                var destination = await connection.QuerySingleAsync<string>("SELECT DestinationUrl FROM SkybrudRedirects WHERE Url = @Url", new { Url = (original + suffix) }).ConfigureAwait(false);

                Assert.Equal((revised + suffix), destination);
            }
        }

        public void Dispose() => _scope.Dispose();
    }
}
