using System;
using System.Threading.Tasks;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Comments
{
    [Collection(IntegrationTestConstants.DataSourceIntegrationTestCollection)]
    public class SqlServerMatchCommentsDataSourceTests
    {
        private readonly SqlServerDataSourceFixture _databaseFixture;

        public SqlServerMatchCommentsDataSourceTests(SqlServerDataSourceFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_total_comments_supports_filter_by_match()
        {
            var commentsDataSource = new SqlServerMatchCommentsDataSource(_databaseFixture.ConnectionFactory);

            foreach (var match in _databaseFixture.Matches)
            {
                var result = await commentsDataSource.ReadTotalComments(match.MatchId.Value).ConfigureAwait(false);

                Assert.Equal(match.Comments.Count, result);
            }
        }
    }
}
