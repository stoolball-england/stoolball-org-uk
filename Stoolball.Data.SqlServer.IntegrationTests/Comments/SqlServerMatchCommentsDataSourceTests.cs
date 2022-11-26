using System;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Comments
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerMatchCommentsDataSourceTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;

        public SqlServerMatchCommentsDataSourceTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_total_comments_supports_filter_by_match()
        {
            var commentsDataSource = new SqlServerMatchCommentsDataSource(_databaseFixture.ConnectionFactory);

            var foundAtLeastOne = false;
            foreach (var match in _databaseFixture.TestData.Matches)
            {
                var result = await commentsDataSource.ReadTotalComments(match.MatchId!.Value).ConfigureAwait(false);

                Assert.Equal(match.Comments.Count, result);
                foundAtLeastOne = foundAtLeastOne || result > 0;
            }
            Assert.True(foundAtLeastOne);
        }

        [Fact]
        public async Task Read_comments_returns_basic_fields()
        {
            var commentsDataSource = new SqlServerMatchCommentsDataSource(_databaseFixture.ConnectionFactory);

            var foundAtLeastOne = false;
            foreach (var match in _databaseFixture.TestData.Matches)
            {
                var results = await commentsDataSource.ReadComments(match.MatchId!.Value).ConfigureAwait(false);

                Assert.Equal(match.Comments.Count, results.Count);
                foreach (var comment in match.Comments)
                {
                    var result = results.SingleOrDefault(x => x.CommentId == comment.CommentId);
                    Assert.NotNull(result);

                    Assert.Equal(comment.MemberName, result!.MemberName);
                    Assert.Equal(comment.CommentDate, result.CommentDate);
                    Assert.Equal(comment.Comment, result.Comment);
                    foundAtLeastOne = true;
                }
            }
            Assert.True(foundAtLeastOne);
        }

        [Fact]
        public async Task Read_comments_returns_newest_first()
        {
            var commentsDataSource = new SqlServerMatchCommentsDataSource(_databaseFixture.ConnectionFactory);

            var foundAtLeastOne = false;
            foreach (var match in _databaseFixture.TestData.Matches)
            {
                var results = await commentsDataSource.ReadComments(match.MatchId!.Value).ConfigureAwait(false);

                var previousCommentDate = DateTimeOffset.MaxValue;
                foreach (var result in results)
                {
                    Assert.True(result.CommentDate <= previousCommentDate);
                    previousCommentDate = result.CommentDate;
                    foundAtLeastOne = true;
                }
            }
            Assert.True(foundAtLeastOne);
        }
    }
}
