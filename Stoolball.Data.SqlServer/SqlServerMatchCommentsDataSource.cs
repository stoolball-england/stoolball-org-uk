using System;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Matches;
using static Stoolball.Data.SqlServer.Constants;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Gets stoolball match comments from the Umbraco database
    /// </summary>
    public class SqlServerMatchCommentsDataSource : ICommentsDataSource<Match>
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
                return await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.MatchComment} WHERE MatchId = @MatchId", new { MatchId = entityId }).ConfigureAwait(false);
            }
        }
    }
}
