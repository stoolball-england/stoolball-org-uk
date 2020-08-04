using Dapper;
using Stoolball.MatchLocations;
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
        /// Gets the number of clubs and teams that match a query
        /// </summary>
        /// <returns></returns>
        public async Task<int> ReadTotalTeams(TeamQuery teamQuery)
        {
            try
            {
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    var (teamWhere, teamParameters) = BuildTeamWhereClause(teamQuery);
                    var (clubWhere, clubParameters) = BuildClubWhereQuery(teamQuery);

                    var parameters = teamParameters;
                    foreach (var key in clubParameters.Keys)
                    {
                        if (!parameters.ContainsKey(key))
                        {
                            parameters.Add(key, clubParameters[key]);
                        }
                    }

                    return await connection.ExecuteScalarAsync<int>($@"SELECT COUNT(TeamListingId) FROM (           
                                    SELECT t.TeamId AS TeamListingId
                                    FROM { Tables.Team } AS t
                                    INNER JOIN { Tables.TeamName } AS tn ON t.TeamId = tn.TeamId AND tn.UntilDate IS NULL
                                    LEFT JOIN { Tables.TeamMatchLocation } AS tml ON tml.TeamId = t.TeamId
                                    LEFT JOIN { Tables.MatchLocation } AS ml ON ml.MatchLocationId = tml.MatchLocationId
                                    {teamWhere}
                                    UNION 
                                    SELECT c.ClubId AS TeamListingId
                                    FROM { Tables.Club } AS c
                                    INNER JOIN { Tables.ClubName } AS cn ON c.ClubId = cn.ClubId AND cn.UntilDate IS NULL
                                    LEFT JOIN { Tables.Team } AS ct ON c.ClubId = ct.ClubId AND ct.UntilYear IS NULL
                                    LEFT JOIN { Tables.TeamMatchLocation } AS tml ON tml.TeamId = ct.TeamId
                                    LEFT JOIN { Tables.MatchLocation } AS ml ON ml.MatchLocationId = tml.MatchLocationId
                                    {clubWhere}
                                ) as Total", new DynamicParameters(parameters)).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(typeof(SqlServerTeamListingDataSource), ex);
                throw;
            }
        }

        /// <summary>
        /// Gets a list of clubs and teams based on a query
        /// </summary>
        /// <returns>A list of <see cref="TeamListing"/> objects. An empty list if no clubs or teams are found.</returns>
        public async Task<List<TeamListing>> ReadTeamListings(TeamQuery teamQuery)
        {
            if (teamQuery is null)
            {
                throw new ArgumentNullException(nameof(teamQuery));
            }

            try
            {
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    var (teamWhere, teamParameters) = BuildTeamWhereClause(teamQuery);
                    var (clubWhere, clubParameters) = BuildClubWhereQuery(teamQuery);

                    var parameters = teamParameters;
                    foreach (var key in clubParameters.Keys)
                    {
                        if (!parameters.ContainsKey(key))
                        {
                            parameters.Add(key, clubParameters[key]);
                        }
                    }

                    // Each result might return multiple rows, therefore to get the correct number of paged results we need a version of the query that returns one row per result.
                    // Because it's a UNION query this inner query will end up getting used twice, hence building it as a separate variable.
                    //
                    // The inner query has three levels:
                    // 1. outer level selects only the id, so that it can be used in a WHERE... IN( ) clause
                    // 2. mid level runs the sort and selects the paged result - even though this selects the same fields as the inner level, 
                    //    putting this paging on the inner level doesn't work
                    // 3. inner level selects fields which are later used for sorting by the outer query
                    var innerQuery = $@"SELECT TeamListingId FROM 
                                            (SELECT TeamListingId, ClubOrTeamName, Active FROM (
                                                SELECT t.TeamId AS TeamListingId, tn.TeamName AS ClubOrTeamName, CASE WHEN UntilYear IS NULL THEN 1 ELSE 0 END AS Active
                                                FROM { Tables.Team } AS t
                                                INNER JOIN { Tables.TeamName } AS tn ON t.TeamId = tn.TeamId AND tn.UntilDate IS NULL
                                                LEFT JOIN { Tables.TeamMatchLocation } AS tml ON tml.TeamId = t.TeamId
                                                LEFT JOIN { Tables.MatchLocation } AS ml ON ml.MatchLocationId = tml.MatchLocationId
                                                {teamWhere}
                                                UNION
                                                SELECT c.ClubId AS TeamListingId, cn.ClubName AS ClubOrTeamName,
                                                CASE WHEN(SELECT COUNT(TeamId) FROM { Tables.Team } WHERE ClubId = c.ClubId) > 0 THEN 1 ELSE 0 END AS Active
                                                FROM { Tables.Club } AS c
                                                INNER JOIN { Tables.ClubName } AS cn ON c.ClubId = cn.ClubId AND cn.UntilDate IS NULL
                                                LEFT JOIN { Tables.Team } AS ct ON c.ClubId = ct.ClubId AND ct.UntilYear IS NULL
                                                LEFT JOIN { Tables.TeamMatchLocation } AS tml ON tml.TeamId = ct.TeamId
                                                LEFT JOIN { Tables.MatchLocation } AS ml ON ml.MatchLocationId = tml.MatchLocationId
                                                {clubWhere}
                                            ) AS MatchingRecords
                                        ORDER BY Active DESC, ClubOrTeamName
                                        OFFSET {(teamQuery.PageNumber - 1) * teamQuery.PageSize} ROWS FETCH NEXT {teamQuery.PageSize} ROWS ONLY) AS MatchingIds";

                    // Now that the inner query can select just the paged ids we need, the outer query can use them to select complete data sets including multiple rows
                    // based on the matching ids rather than directly on the paging criteria
                    var outerQuery = $@"SELECT t.TeamId AS TeamListingId, tn.TeamName AS ClubOrTeamName, t.TeamRoute AS ClubOrTeamRoute, CASE WHEN UntilYear IS NULL THEN 1 ELSE 0 END AS Active,
                                t.PlayerType, 
                                ml.Locality, ml.Town, ml.MatchLocationRoute
                                FROM { Tables.Team } AS t 
                                INNER JOIN { Tables.TeamName } AS tn ON t.TeamId = tn.TeamId AND tn.UntilDate IS NULL
                                LEFT JOIN { Tables.TeamMatchLocation } AS tml ON tml.TeamId = t.TeamId
                                LEFT JOIN { Tables.MatchLocation } AS ml ON ml.MatchLocationId = tml.MatchLocationId 
                                WHERE t.TeamId IN ({innerQuery})
                                UNION
                                SELECT c.ClubId AS TeamListingId, cn.ClubName AS ClubOrTeamName, c.ClubRoute AS ClubOrTeamRoute, 
                                CASE WHEN (SELECT COUNT(TeamId) FROM { Tables.Team } WHERE ClubId = c.ClubId) > 0 THEN 1 ELSE 0 END AS Active,
                                ct.PlayerType,
                                ml.Locality, ml.Town, ml.MatchLocationRoute
                                FROM { Tables.Club } AS c 
                                INNER JOIN { Tables.ClubName } AS cn ON c.ClubId = cn.ClubId AND cn.UntilDate IS NULL
                                LEFT JOIN { Tables.Team } AS ct ON c.ClubId = ct.ClubId AND ct.UntilYear IS NULL
                                LEFT JOIN { Tables.TeamMatchLocation } AS tml ON tml.TeamId = ct.TeamId
                                LEFT JOIN { Tables.MatchLocation } AS ml ON ml.MatchLocationId = tml.MatchLocationId 
                                WHERE c.ClubId IN ({innerQuery})
                                ORDER BY Active DESC, ClubOrTeamName";

                    var teamListings = await connection.QueryAsync<TeamListing, string, MatchLocation, TeamListing>(outerQuery,
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

        private static (string where, Dictionary<string, object> parameters) BuildTeamWhereClause(TeamQuery teamQuery)
        {
            var where = new List<string>();
            var parameters = new Dictionary<string, object>();

            where.Add($"t.ClubId IS NULL AND NOT t.TeamType = '{TeamType.Transient.ToString()}'");
            if (!string.IsNullOrEmpty(teamQuery?.Query))
            {
                where.Add("(tn.TeamName LIKE @Query OR t.PlayerType LIKE @Query OR ml.Locality LIKE @Query OR ml.Town LIKE @Query OR ml.AdministrativeArea LIKE @Query)");
                parameters.Add("@Query", $"%{teamQuery.Query}%");
            }

            return ("WHERE " + string.Join(" AND ", where), parameters);
        }

        private static (string where, Dictionary<string, object> parameters) BuildClubWhereQuery(TeamQuery teamQuery)
        {
            var where = new List<string>();
            var parameters = new Dictionary<string, object>();

            where.Add($"NOT ct.TeamType = '{TeamType.Transient.ToString()}'");
            if (!string.IsNullOrEmpty(teamQuery?.Query))
            {
                where.Add("(cn.ClubName LIKE @Query OR ct.PlayerType LIKE @Query OR ml.Locality LIKE @Query OR ml.Town LIKE @Query OR ml.AdministrativeArea LIKE @Query)");
                parameters.Add("@Query", $"%{teamQuery.Query}%");
            }

            return ("WHERE " + string.Join(" AND ", where), parameters);
        }
    }
}
