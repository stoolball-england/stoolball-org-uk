using System;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Comments
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerTournamentCommentsDataSourceTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;

        public SqlServerTournamentCommentsDataSourceTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_total_comments_supports_filter_by_tournament()
        {
            var commentsDataSource = new SqlServerTournamentCommentsDataSource(_databaseFixture.ConnectionFactory);
            var commentsFound = 0;

            foreach (var tournament in _databaseFixture.TestData.Tournaments)
            {
                var result = await commentsDataSource.ReadTotalComments(tournament.TournamentId!.Value).ConfigureAwait(false);

                Assert.Equal(tournament.Comments.Count, result);
                commentsFound += result;
            }

            Assert.True(commentsFound > 0);
        }

        [Fact]
        public async Task Read_comments_returns_basic_fields()
        {
            var commentsDataSource = new SqlServerTournamentCommentsDataSource(_databaseFixture.ConnectionFactory);
            var commentsFound = false;

            foreach (var tournament in _databaseFixture.TestData.Tournaments)
            {
                var results = await commentsDataSource.ReadComments(tournament.TournamentId!.Value).ConfigureAwait(false);

                Assert.Equal(tournament.Comments.Count, results.Count);
                foreach (var comment in tournament.Comments)
                {
                    var result = results.SingleOrDefault(x => x.CommentId == comment.CommentId);
                    Assert.NotNull(result);

                    Assert.Equal(comment.MemberName, result!.MemberName);
                    Assert.Equal(comment.CommentDate, result.CommentDate);
                    Assert.Equal(comment.Comment, result.Comment);
                    commentsFound = true;
                }
            }

            Assert.True(commentsFound);
        }

        [Fact]
        public async Task Read_comments_returns_newest_first()
        {
            var commentsDataSource = new SqlServerTournamentCommentsDataSource(_databaseFixture.ConnectionFactory);
            var commentsFound = false;

            foreach (var tournament in _databaseFixture.TestData.Tournaments)
            {
                var results = await commentsDataSource.ReadComments(tournament.TournamentId!.Value).ConfigureAwait(false);

                var previousCommentDate = DateTimeOffset.MaxValue;
                foreach (var result in results)
                {
                    Assert.True(result.CommentDate <= previousCommentDate);
                    previousCommentDate = result.CommentDate;
                    commentsFound = true;
                }
            }

            Assert.True(commentsFound);
        }
    }
}
