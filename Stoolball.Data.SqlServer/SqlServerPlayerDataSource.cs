﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Teams;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Gets stoolball player data from the Umbraco database
    /// </summary>
    public class SqlServerPlayerDataSource : IPlayerDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;

        public SqlServerPlayerDataSource(IDatabaseConnectionFactory databaseConnectionFactory)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
        }

        /// <summary>
        /// Gets a list of player identities based on a query
        /// </summary>
        /// <returns>A list of <see cref="PlayerIdentity"/> objects. An empty list if no player identities are found.</returns>
        public async Task<List<PlayerIdentity>> ReadPlayerIdentities(PlayerIdentityQuery playerQuery)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var sql = $@"SELECT p.PlayerIdentityId, p.PlayerIdentityName, p.TotalMatches, p.FirstPlayed, p.LastPlayed,
                            t.TeamId, tn.TeamName
                            FROM {Tables.PlayerIdentity} AS p 
                            INNER JOIN {Tables.Team} AS t ON p.TeamId = t.TeamId
                            INNER JOIN {Tables.TeamName} AS tn ON t.TeamId = tn.TeamId AND tn.UntilDate IS NULL
                            <<WHERE>>
                            ORDER BY t.TeamId ASC, p.Probability DESC, p.PlayerIdentityName ASC";

                var where = new List<string>();
                var parameters = new Dictionary<string, object>();

                if (!string.IsNullOrEmpty(playerQuery?.Query))
                {
                    where.Add("p.PlayerIdentityName LIKE @Query");
                    parameters.Add("@Query", $"%{playerQuery.Query.Replace(" ", "%")}%");
                }

                if (playerQuery?.TeamIds?.Count > 0)
                {
                    where.Add("p.TeamId IN @TeamIds");
                    parameters.Add("@TeamIds", playerQuery.TeamIds.Select(x => x.ToString()));
                }

                sql = sql.Replace("<<WHERE>>", where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty);

                return (await connection.QueryAsync<PlayerIdentity, Team, PlayerIdentity>(sql,
                    (player, team) =>
                    {
                        player.Team = team;
                        return player;
                    },
                    new DynamicParameters(parameters),
                    splitOn: "TeamId").ConfigureAwait(false)).ToList();
            }
        }
    }
}