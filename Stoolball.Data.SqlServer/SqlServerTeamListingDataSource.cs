using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Gets stoolball club and team data from the Umbraco database
    /// </summary>
    public class SqlServerTeamListingDataSource : ITeamListingDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;

        public SqlServerTeamListingDataSource(IDatabaseConnectionFactory databaseConnectionFactory)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
        }

        /// <summary>
        /// Gets the number of clubs and teams that match a query
        /// </summary>
        /// <returns></returns>
        public async Task<int> ReadTotalTeams(TeamQuery teamQuery)
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
                                    INNER JOIN { Tables.TeamVersion } AS tn ON t.TeamId = tn.TeamId
                                    LEFT JOIN { Tables.TeamMatchLocation } AS tml ON tml.TeamId = t.TeamId AND tml.UntilDate IS NULL
                                    LEFT JOIN { Tables.MatchLocation } AS ml ON ml.MatchLocationId = tml.MatchLocationId
                                    {teamWhere}
                                    AND tn.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = t.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                                    UNION 
                                    SELECT c.ClubId AS TeamListingId
                                    FROM { Tables.Club } AS c
                                    INNER JOIN { Tables.ClubVersion } AS cn ON c.ClubId = cn.ClubId
                                    LEFT JOIN { Tables.Team } AS ct ON c.ClubId = ct.ClubId
                                    LEFT JOIN { Tables.TeamVersion } AS ctn ON ct.TeamId = ctn.TeamId
                                    LEFT JOIN { Tables.TeamMatchLocation } AS tml ON tml.TeamId = ct.TeamId AND tml.UntilDate IS NULL
                                    LEFT JOIN { Tables.MatchLocation } AS ml ON ml.MatchLocationId = tml.MatchLocationId
                                    {clubWhere}
                                    AND cn.ClubVersionId = (SELECT TOP 1 ClubVersionId FROM {Tables.ClubVersion} WHERE ClubId = c.ClubId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                                    AND ctn.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = ct.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                                ) as Total", new DynamicParameters(parameters)).ConfigureAwait(false);
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
                                                SELECT t.TeamId AS TeamListingId, tn.TeamName AS ClubOrTeamName, CASE WHEN tn.UntilDate IS NULL THEN 1 ELSE 0 END AS Active
                                                FROM { Tables.Team } AS t
                                                INNER JOIN { Tables.TeamVersion } AS tn ON t.TeamId = tn.TeamId
                                                LEFT JOIN { Tables.TeamMatchLocation } AS tml ON tml.TeamId = t.TeamId AND tml.UntilDate IS NULL
                                                LEFT JOIN { Tables.MatchLocation } AS ml ON ml.MatchLocationId = tml.MatchLocationId
                                                {teamWhere}
                                                AND tn.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = t.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                                                UNION
                                                SELECT c.ClubId AS TeamListingId, cn.ClubName AS ClubOrTeamName,
                                                CASE WHEN(SELECT COUNT(t2.TeamId) FROM { Tables.Team } t2 INNER JOIN { Tables.TeamVersion } tn2 ON t2.TeamId = tn2.TeamId WHERE ClubId = c.ClubId AND tn2.UntilDate IS NULL) > 0 THEN 1 ELSE 0 END AS Active
                                                FROM { Tables.Club } AS c
                                                INNER JOIN { Tables.ClubVersion } AS cn ON c.ClubId = cn.ClubId
                                                LEFT JOIN { Tables.Team } AS ct ON c.ClubId = ct.ClubId
                                                LEFT JOIN { Tables.TeamVersion } AS ctn ON ct.TeamId = ctn.TeamId
                                                LEFT JOIN { Tables.TeamMatchLocation } AS tml ON tml.TeamId = ct.TeamId AND tml.UntilDate IS NULL
                                                LEFT JOIN { Tables.MatchLocation } AS ml ON ml.MatchLocationId = tml.MatchLocationId
                                                {clubWhere}
                                                AND cn.ClubVersionId = (SELECT TOP 1 ClubVersionId FROM {Tables.ClubVersion} WHERE ClubId = c.ClubId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                                                AND ctn.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = ct.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                                            ) AS MatchingRecords
                                        ORDER BY Active DESC, ClubOrTeamName
                                        OFFSET {(teamQuery.PageNumber - 1) * teamQuery.PageSize} ROWS FETCH NEXT {teamQuery.PageSize} ROWS ONLY) AS MatchingIds";

                // Now that the inner query can select just the paged ids we need, the outer query can use them to select complete data sets including multiple rows
                // based on the matching ids rather than directly on the paging criteria
                var outerQuery = $@"SELECT t.TeamId AS TeamListingId, tn.TeamName AS ClubOrTeamName, t.TeamType, t.TeamRoute AS ClubOrTeamRoute, CASE WHEN tn.UntilDate IS NULL THEN 1 ELSE 0 END AS Active,
                                t.PlayerType, 
                                ml.Locality, ml.Town, ml.MatchLocationRoute
                                FROM { Tables.Team } AS t 
                                INNER JOIN { Tables.TeamVersion } AS tn ON t.TeamId = tn.TeamId
                                LEFT JOIN { Tables.TeamMatchLocation } AS tml ON tml.TeamId = t.TeamId AND tml.UntilDate IS NULL
                                LEFT JOIN { Tables.MatchLocation } AS ml ON ml.MatchLocationId = tml.MatchLocationId 
                                WHERE t.TeamId IN ({innerQuery})
                                AND tn.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = t.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                                UNION
                                SELECT c.ClubId AS TeamListingId, cn.ClubName AS ClubOrTeamName, NULL AS TeamType, c.ClubRoute AS ClubOrTeamRoute, 
                                CASE WHEN (SELECT COUNT(t2.TeamId) FROM { Tables.Team } t2 INNER JOIN { Tables.TeamVersion } tn2 ON t2.TeamId = tn2.TeamId WHERE ClubId = c.ClubId AND tn2.UntilDate IS NULL) > 0 THEN 1 ELSE 0 END AS Active,
                                ct.PlayerType,
                                ml.Locality, ml.Town, ml.MatchLocationRoute
                                FROM { Tables.Club } AS c 
                                INNER JOIN { Tables.ClubVersion } AS cn ON c.ClubId = cn.ClubId 
                                LEFT JOIN { Tables.Team } AS ct ON c.ClubId = ct.ClubId
                                LEFT JOIN { Tables.TeamVersion } AS ctn ON ct.TeamId = ctn.TeamId
                                LEFT JOIN { Tables.TeamMatchLocation } AS tml ON tml.TeamId = ct.TeamId AND tml.UntilDate IS NULL
                                LEFT JOIN { Tables.MatchLocation } AS ml ON ml.MatchLocationId = tml.MatchLocationId 
                                WHERE c.ClubId IN ({innerQuery})
                                AND cn.ClubVersionId = (SELECT TOP 1 ClubVersionId FROM {Tables.ClubVersion} WHERE ClubId = c.ClubId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                                AND ctn.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = ct.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
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
                    resolvedTeam.PlayerTypes = copiesOfTeam.SelectMany(listing => listing.PlayerTypes).OfType<PlayerType>().Distinct().ToList();
                    resolvedTeam.MatchLocations = copiesOfTeam.Select(listing => listing.MatchLocations.SingleOrDefault()).OfType<MatchLocation>().Distinct(new MatchLocationEqualityComparer()).ToList();
                    return resolvedTeam;
                }).ToList();

                return resolvedListings;
            }
        }

        private static (string where, Dictionary<string, object> parameters) BuildTeamWhereClause(TeamQuery teamQuery)
        {
            var where = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(teamQuery?.Query))
            {
                where.Add("(tn.TeamName LIKE @Query OR t.PlayerType LIKE @Query OR ml.Locality LIKE @Query OR ml.Town LIKE @Query OR ml.AdministrativeArea LIKE @Query)");
                parameters.Add("@Query", $"%{teamQuery.Query}%");
            }

            if (teamQuery?.TeamTypes?.Count > 0)
            {
                where.Add("t.TeamType IN @TeamTypes");
                parameters.Add("@TeamTypes", teamQuery.TeamTypes.Select(x => x.ToString()));
            }

            // For listings, clubs with one active team are treated like a team without a club, so that the team is returned
            if (teamQuery != null && !teamQuery.IncludeClubTeams)
            {
                where.Add($"(t.ClubId IS NULL OR (SELECT COUNT({Tables.Team}.TeamId) FROM {Tables.Team} INNER JOIN {Tables.TeamVersion} ON {Tables.Team}.TeamId = {Tables.TeamVersion}.TeamId WHERE ClubId = t.ClubId AND {Tables.TeamVersion}.UntilDate IS NULL) = 1)");
            }

            return (where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "WHERE 1=1", parameters); // Always have a where clause so that it can be appended to
        }

        private static (string where, Dictionary<string, object> parameters) BuildClubWhereQuery(TeamQuery teamQuery)
        {
            var where = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(teamQuery?.Query))
            {
                where.Add("(cn.ClubName LIKE @Query OR ct.PlayerType LIKE @Query OR ml.Locality LIKE @Query OR ml.Town LIKE @Query OR ml.AdministrativeArea LIKE @Query)");
                parameters.Add("@Query", $"%{teamQuery.Query}%");
            }

            if (teamQuery?.TeamTypes?.Count > 0)
            {
                where.Add("(ct.TeamType IN @TeamTypes OR ct.TeamType IS NULL)");
                parameters.Add("@TeamTypes", teamQuery.TeamTypes.Select(x => x.ToString()));
            }

            // For listings, clubs with one active team are treated like a team without a club, so that the team is returned
            if (teamQuery != null && !teamQuery.IncludeClubTeams)
            {
                where.Add($"(SELECT COUNT({Tables.Team}.TeamId) FROM {Tables.Team} INNER JOIN {Tables.TeamVersion} ON {Tables.Team}.TeamId = {Tables.TeamVersion}.TeamId WHERE ClubId = c.ClubId AND {Tables.TeamVersion}.UntilDate IS NULL) != 1");
            }

            return (where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "WHERE 1=1", parameters); // Always have a where clause so that it can be appended to
        }
    }
}
