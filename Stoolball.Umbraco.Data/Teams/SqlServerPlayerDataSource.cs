using Dapper;
using Stoolball.Teams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Umbraco.Data.Teams
{
    /// <summary>
    /// Gets stoolball player data from the Umbraco database
    /// </summary>
    public class SqlServerPlayerDataSource : IPlayerDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly ILogger _logger;

        public SqlServerPlayerDataSource(IDatabaseConnectionFactory databaseConnectionFactory, ILogger logger)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets a list of player identities based on a query
        /// </summary>
        /// <returns>A list of <see cref="PlayerIdentity"/> objects. An empty list if no player identities are found.</returns>
        public async Task<List<PlayerIdentity>> ReadPlayerIdentities(PlayerIdentityQuery playerQuery)
        {
            try
            {
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    var sql = $@"SELECT p.PlayerIdentityId
                            FROM {Tables.PlayerIdentity} AS p 
                            <<WHERE>>";

                    var where = new List<string>();
                    var parameters = new Dictionary<string, object>();

                    if (playerQuery?.TeamIds?.Count > 0)
                    {
                        where.Add("p.TeamId IN @TeamIds");
                        parameters.Add("@TeamIds", playerQuery.TeamIds.Select(x => x.ToString()));
                    }

                    if (playerQuery?.PlayerRoles?.Count > 0)
                    {
                        where.Add("p.PlayerRole IN @PlayerRoles");
                        parameters.Add("@PlayerRoles", playerQuery.PlayerRoles.Select(x => x.ToString()));
                    }

                    sql = sql.Replace("<<WHERE>>", where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty);

                    return (await connection.QueryAsync<PlayerIdentity>(sql, new DynamicParameters(parameters)).ConfigureAwait(false)).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(typeof(SqlServerPlayerDataSource), ex);
                throw;
            }
        }
    }
}
