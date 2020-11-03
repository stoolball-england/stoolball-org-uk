using System;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Matches;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Umbraco.Data.Matches
{
    /// <summary>
    /// Gets stoolball tournament comments from the Umbraco database
    /// </summary>
    public class SqlServerTournamentCommentsDataSource : ICommentsDataSource<Tournament>
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
        public async Task<int> ReadTotalComments(Guid tournamentId)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.TournamentComment} WHERE TournamentId = @TournamentId", new { TournamentId = tournamentId }).ConfigureAwait(false);
            }
        }
    }
}
