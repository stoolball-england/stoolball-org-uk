using Dapper;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Umbraco.Data.Teams
{
    /// <summary>
    /// Gets stoolball club and team data from the Umbraco database
    /// </summary>
    public class SqlServerTeamListingDataSource : ITeamListingDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly ILogger _logger;

        public SqlServerTeamListingDataSource(IDatabaseConnectionFactory databaseConnectionFactory, ILogger logger)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets a list of clubs and teams based on a query
        /// </summary>
        /// <returns>A list of <see cref="TeamListing"/> objects. An empty list if no clubs or teams are found.</returns>
        public async Task<List<TeamListing>> ReadTeamListings(TeamQuery teamQuery)
        {
            try
            {
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    var sql = new StringBuilder();
                    var parameters = new Dictionary<string, object>();

                    var (teamSql, teamParameters) = ApplyTeamQuery(teamQuery,
                         $@"SELECT t.TeamId AS TeamListingId, tn.TeamName AS ClubOrTeamName, t.TeamRoute AS ClubOrTeamRoute, CASE WHEN UntilYear IS NULL THEN 1 ELSE 0 END AS Active,
                            t.PlayerType, 
                            ml.Locality, ml.Town, ml.MatchLocationRoute
                            FROM {Tables.Team} AS t 
                            INNER JOIN {Tables.TeamName} AS tn ON t.TeamId = tn.TeamId AND tn.UntilDate IS NULL
                            LEFT JOIN {Tables.TeamMatchLocation} AS tml ON tml.TeamId = t.TeamId
                            LEFT JOIN {Tables.MatchLocation} AS ml ON ml.MatchLocationId = tml.MatchLocationId 
                            <<WHERE>>");

                    sql.Append(teamSql);
                    parameters = teamParameters;

                    sql.Append(" UNION ");

                    var (clubSql, clubParameters) = ApplyClubQuery(teamQuery, $@"SELECT c.ClubId AS TeamListingId, cn.ClubName AS ClubOrTeamName, c.ClubRoute AS ClubOrTeamRoute, 
                            CASE WHEN (SELECT COUNT(TeamId) FROM {Tables.Team} WHERE ClubId = c.ClubId) > 0 THEN 1 ELSE 0 END AS Active,
                            ct.PlayerType,
                            ml.Locality, ml.Town, ml.MatchLocationRoute
                            FROM {Tables.Club} AS c 
                            INNER JOIN {Tables.ClubName} AS cn ON c.ClubId = cn.ClubId AND cn.UntilDate IS NULL
                            LEFT JOIN {Tables.Team} AS ct ON c.ClubId = ct.ClubId AND ct.UntilYear IS NULL
                            LEFT JOIN {Tables.TeamMatchLocation} AS tml ON tml.TeamId = ct.TeamId
                            LEFT JOIN {Tables.MatchLocation} AS ml ON ml.MatchLocationId = tml.MatchLocationId 
                            <<WHERE>>");

                    sql.Append(clubSql);
                    foreach (var key in clubParameters.Keys)
                    {
                        if (!parameters.ContainsKey(key))
                        {
                            parameters.Add(key, clubParameters[key]);
                        }
                    }

                    sql.Append(@" ORDER BY Active DESC, ClubOrTeamName");

                    var teamListings = await connection.QueryAsync<TeamListing, string, MatchLocation, TeamListing>(sql.ToString(),
                        (teamListing, playerType, matchLocation) =>
                        {
                            if (!string.IsNullOrEmpty(playerType))
                            {
                                teamListing.PlayerTypes.Add((PlayerType)Enum.Parse(typeof(PlayerType), playerType));
                            }
                            if (matchLocation != null)
                            {
                                teamListing.MatchLocations.Add(matchLocation);
                            }
                            return teamListing;
                        },
                        new DynamicParameters(parameters),
                        splitOn: "PlayerType, Locality").ConfigureAwait(false);

                    var resolvedListings = teamListings.GroupBy(team => team.TeamListingId).Select(copiesOfTeam =>
                    {
                        var resolvedTeam = copiesOfTeam.First();
                        resolvedTeam.PlayerTypes = copiesOfTeam.Select(listing => listing.PlayerTypes.SingleOrDefault()).OfType<PlayerType>().Distinct().ToList();
                        resolvedTeam.MatchLocations = copiesOfTeam.Select(listing => listing.MatchLocations.SingleOrDefault()).OfType<MatchLocation>().Distinct(new MatchLocationEqualityComparer()).ToList();
                        return resolvedTeam;
                    }).ToList();

                    return resolvedListings;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(typeof(SqlServerTeamDataSource), ex);
                throw;
            }
        }

        private static (string filteredSql, Dictionary<string, object> parameters) ApplyTeamQuery(TeamQuery teamQuery, string sql)
        {
            var where = new List<string>();
            var parameters = new Dictionary<string, object>();

            where.Add($"t.ClubId IS NULL AND NOT t.TeamType = '{TeamType.Transient.ToString()}'");
            if (!string.IsNullOrEmpty(teamQuery?.Query))
            {
                where.Add("(tn.TeamName LIKE @Query OR ml.Locality LIKE @Query OR ml.Town LIKE @Query OR ml.AdministrativeArea LIKE @Query)");
                parameters.Add("@Query", $"%{teamQuery.Query}%");
            }

            sql = sql.Replace("<<WHERE>>", "WHERE " + string.Join(" AND ", where));

            return (sql, parameters);
        }

        private static (string filteredSql, Dictionary<string, object> parameters) ApplyClubQuery(TeamQuery teamQuery, string sql)
        {
            var where = new List<string>();
            var parameters = new Dictionary<string, object>();

            where.Add($"NOT ct.TeamType = '{TeamType.Transient.ToString()}'");
            if (!string.IsNullOrEmpty(teamQuery?.Query))
            {
                where.Add("cn.ClubName LIKE @Query");
                parameters.Add("@Query", $"%{teamQuery.Query}%");
            }

            sql = sql.Replace("<<WHERE>>", "WHERE " + string.Join(" AND ", where));

            return (sql, parameters);
        }
    }
}
