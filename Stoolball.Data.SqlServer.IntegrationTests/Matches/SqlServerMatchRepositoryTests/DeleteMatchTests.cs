using System;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches.SqlServerMatchRepositoryTests
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class DeleteMatchTests : MatchRepositoryTestsBase, IDisposable
    {
        public DeleteMatchTests(SqlServerTestDataFixture databaseFixture) : base(databaseFixture) { }

        [Fact]
        public async Task Delete_match_succeeds()
        {
            await Repository.DeleteMatch(DatabaseFixture.TestData.MatchInThePastWithFullDetails!, MemberKey, MemberName).ConfigureAwait(false);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT MatchId FROM {Tables.Match} WHERE MatchId = @MatchId", new { DatabaseFixture.TestData.MatchInThePastWithFullDetails!.MatchId }).ConfigureAwait(false);
                Assert.Null(result);
            }
        }
    }
}
