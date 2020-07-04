using Dapper;
using Stoolball.Matches;
using System;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Umbraco.Data.Matches
{
    /// <summary>
    /// Gets stoolball tournament comments from the Umbraco database
    /// </summary>
    public class SqlServerTournamentCommentsDataSource : ICommentsDataSource<Tournament>
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly ILogger _logger;

        public SqlServerTournamentCommentsDataSource(IDatabaseConnectionFactory databaseConnectionFactory, ILogger logger)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the number of comments on a tournament
        /// </summary>
        /// <returns></returns>
        public async Task<int> ReadTotalComments(Guid tournamentId)
        {
            try
            {
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    return await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.TournamentComment} WHERE TournamentId = @TournamentId", new { TournamentId = tournamentId }).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(typeof(SqlServerTournamentDataSource), ex);
                throw;
            }
        }
    }
}
