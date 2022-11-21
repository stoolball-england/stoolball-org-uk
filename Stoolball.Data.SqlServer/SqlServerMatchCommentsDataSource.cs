using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Comments;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Gets stoolball match comments from the Umbraco database
    /// </summary>
    public class SqlServerMatchCommentsDataSource : ICommentsDataSource<Match>, ICacheableCommentsDataSource<Match>
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;

        public SqlServerMatchCommentsDataSource(IDatabaseConnectionFactory databaseConnectionFactory)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
        }

        /// <summary>
        /// Gets the number of comments on a match
        /// </summary>
        /// <returns></returns>
        public async Task<int> ReadTotalComments(Guid entityId)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Comment} WHERE MatchId = @MatchId", new { MatchId = entityId }).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task<List<HtmlComment>> ReadComments(Guid entityId)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return (await connection.QueryAsync<HtmlComment>(
                            $@"SELECT c.CommentId, c.CommentDate, c.Comment, 
                               n.text AS MemberName, m.Email AS MemberEmail
                               FROM {Tables.Comment} c LEFT JOIN {Tables.UmbracoNode} n ON c.MemberKey = n.uniqueId 
                               LEFT JOIN {Tables.UmbracoMember} m ON n.id = m.nodeId
                               WHERE MatchId = @MatchId ORDER BY CommentDate DESC", new { MatchId = entityId }).ConfigureAwait(false)).ToList();
            }
        }
    }
}
