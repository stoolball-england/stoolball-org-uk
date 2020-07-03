using Dapper;
using System;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Umbraco.Data.Matches
{
    /// <summary>
    /// Gets stoolball match comments from the Umbraco database
    /// </summary>
    public class SqlServerMatchCommentsDataSource : IMatchCommentsDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly ILogger _logger;

        public SqlServerMatchCommentsDataSource(IDatabaseConnectionFactory databaseConnectionFactory, ILogger logger)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the number of comments on a match
        /// </summary>
        /// <returns></returns>
        public async Task<int> ReadTotalComments(Guid matchId)
        {
            try
            {
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    return await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.MatchComment} WHERE MatchId = @MatchId", new { MatchId = matchId }).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(typeof(SqlServerMatchDataSource), ex);
                throw;
            }
        }
    }
}
