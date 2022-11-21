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
    /// Gets stoolball tournament comments from the Umbraco database
    /// </summary>
    public class SqlServerTournamentCommentsDataSource : ICommentsDataSource<Tournament>, ICacheableCommentsDataSource<Tournament>
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;

        public SqlServerTournamentCommentsDataSource(IDatabaseConnectionFactory databaseConnectionFactory)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
        }

        /// <summary>
        /// Gets the number of comments on a tournament
        /// </summary>
        /// <returns></returns>
        public async Task<int> ReadTotalComments(Guid entityId)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Comment} WHERE TournamentId = @TournamentId", new { TournamentId = entityId }).ConfigureAwait(false);
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
                               WHERE TournamentId = @TournamentId ORDER BY CommentDate DESC", new { TournamentId = entityId }).ConfigureAwait(false)).ToList();
            }
        }
    }
}
