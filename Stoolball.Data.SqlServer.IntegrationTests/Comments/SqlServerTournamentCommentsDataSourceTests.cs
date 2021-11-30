using System;
using System.Linq;
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

        [Fact]
        public async Task Read_comments_returns_basic_fields()
        {
            var commentsDataSource = new SqlServerTournamentCommentsDataSource(_databaseFixture.ConnectionFactory);

            foreach (var tournament in _databaseFixture.Tournaments)
            {
                var results = await commentsDataSource.ReadComments(tournament.TournamentId.Value).ConfigureAwait(false);

                Assert.Equal(tournament.Comments.Count, results.Count);
                foreach (var comment in tournament.Comments)
                {
                    var result = results.SingleOrDefault(x => x.CommentId == comment.CommentId);
                    Assert.NotNull(result);

                    Assert.Equal(comment.MemberName, result.MemberName);
                    Assert.Equal(comment.CommentDate, result.CommentDate);
                    Assert.Equal(comment.Comment, result.Comment);
                }
            }
        }

        [Fact]
        public async Task Read_comments_returns_newest_first()
        {
            var commentsDataSource = new SqlServerMatchCommentsDataSource(_databaseFixture.ConnectionFactory);

            foreach (var match in _databaseFixture.Matches)
            {
                var results = await commentsDataSource.ReadComments(match.MatchId.Value).ConfigureAwait(false);

                var previousCommentDate = DateTimeOffset.MaxValue;
                foreach (var result in results)
                {
                    Assert.True(result.CommentDate <= previousCommentDate);
                    previousCommentDate = result.CommentDate;
                }
            }
        }
    }
}
