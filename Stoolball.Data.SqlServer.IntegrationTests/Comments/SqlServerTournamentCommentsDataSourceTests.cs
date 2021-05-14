using System;
using System.Threading.Tasks;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Comments
{
    [Collection(IntegrationTestConstants.DataSourceIntegrationTestCollection)]
    public class SqlServerTournamentCommentsDataSourceTests
    {
        private readonly SqlServerDataSourceFixture _databaseFixture;

        public SqlServerTournamentCommentsDataSourceTests(SqlServerDataSourceFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_total_comments_supports_filter_by_tournament()
        {
            var commentsDataSource = new SqlServerTournamentCommentsDataSource(_databaseFixture.ConnectionFactory);

            foreach (var tournament in _databaseFixture.Tournaments)
            {
                var result = await commentsDataSource.ReadTotalComments(tournament.TournamentId.Value).ConfigureAwait(false);

                Assert.Equal(tournament.Comments.Count, result);
            }
        }
    }
}
