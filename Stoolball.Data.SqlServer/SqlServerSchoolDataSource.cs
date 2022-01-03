using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Routing;
using Stoolball.Schools;
using Stoolball.Teams;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Gets school data from the Umbraco database
    /// </summary>
    public class SqlServerSchoolDataSource : ISchoolDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IRouteNormaliser _routeNormaliser;

        public SqlServerSchoolDataSource(IDatabaseConnectionFactory databaseConnectionFactory, IRouteNormaliser routeNormaliser)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _routeNormaliser = routeNormaliser ?? throw new ArgumentNullException(nameof(routeNormaliser));
        }

        /// <summary>
        /// Gets the number of schools that match a query
        /// </summary>
        /// <returns></returns>
        public async Task<int> ReadTotalSchools(SchoolFilter schoolQuery)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var (where, parameters) = BuildWhereClause(schoolQuery);
                return await connection.ExecuteScalarAsync<int>($@"SELECT COUNT(DISTINCT sc.SchoolId)
                            FROM {Tables.School} AS sc
                            INNER JOIN {Tables.SchoolVersion} AS sv ON sc.SchoolId = sv.SchoolId
                            LEFT JOIN {Tables.Team} t ON sc.SchoolId = t.SchoolId
                            LEFT JOIN {Tables.TeamMatchLocation} tml ON t.TeamId = tml.TeamId
                            LEFT JOIN {Tables.MatchLocation} ml ON tml.MatchLocationid = ml.MatchLocationId
                            {where}
                            AND sv.SchoolVersionId = (SELECT TOP 1 SchoolVersionId FROM {Tables.SchoolVersion} WHERE SchoolId = sc.SchoolId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)",
                            new DynamicParameters(parameters)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets a list of schools based on a query
        /// </summary>
        /// <returns>A list of <see cref="School"/> objects. An empty list if no schools are found.</returns>
        public async Task<List<School>> ReadSchools(SchoolFilter filter)
        {
            if (filter == null) { filter = new SchoolFilter(); }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var (where, parameters) = BuildWhereClause(filter);

                var sql = $@"SELECT sc2.SchoolId, sv2.SchoolName, sc2.SchoolRoute,
                            t.TeamId
                            FROM {Tables.School} AS sc2
                            INNER JOIN {Tables.SchoolVersion} AS sv2 ON sc2.SchoolId = sv2.SchoolId
                            LEFT JOIN {Tables.Team} t ON sc2.SchoolId = t.SchoolId
                            WHERE sc2.SchoolId IN (
                                SELECT SchoolId FROM (
                                    SELECT DISTINCT sc.SchoolId, CASE WHEN sv.UntilDate IS NULL THEN 1 ELSE 0 END AS Active, sv.ComparableName
                                    FROM {Tables.School} AS sc
                                    INNER JOIN {Tables.SchoolVersion} AS sv ON sc.SchoolId = sv.SchoolId
                                    LEFT JOIN {Tables.Team} t ON sc.SchoolId = t.SchoolId
                                    LEFT JOIN {Tables.TeamMatchLocation} tml ON t.TeamId = tml.TeamId
                                    LEFT JOIN {Tables.MatchLocation} ml ON tml.MatchLocationid = ml.MatchLocationId
                                    {where}
                                    AND sv.SchoolVersionId = (SELECT TOP 1 SchoolVersionId FROM {Tables.SchoolVersion} WHERE SchoolId = sc.SchoolId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                                    ORDER BY Active, sv.ComparableName
                                    OFFSET @PageOffset ROWS FETCH NEXT @PageSize ROWS ONLY
                                ) AS SchoolIds
                            )
                            AND sv2.SchoolVersionId = (SELECT TOP 1 SchoolVersionId FROM {Tables.SchoolVersion} WHERE SchoolId = sc2.SchoolId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                            ORDER BY CASE WHEN sv2.UntilDate IS NULL THEN 0 ELSE 1 END, sv2.ComparableName";

                parameters.Add("@PageOffset", (filter.Paging.PageNumber - 1) * filter.Paging.PageSize);
                parameters.Add("@PageSize", filter.Paging.PageSize);

                var schools = await connection.QueryAsync<School, Team, School>(sql,
                    (school, dummyForNow) =>
                    {
                        return school;
                    },
                    new DynamicParameters(parameters),
                    splitOn: "TeamId").ConfigureAwait(false);

                var resolvedSchools = schools.GroupBy(school => school.SchoolId).Select(copiesOfSchool =>
                {
                    var resolvedSchool = copiesOfSchool.First();
                    return resolvedSchool;
                }).ToList();

                return resolvedSchools;
            }
        }

        private static (string sql, Dictionary<string, object> parameters) BuildWhereClause(SchoolFilter filter)
        {
            var where = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(filter?.Query))
            {
                where.Add("(sv.SchoolName LIKE @Query OR ml.Locality LIKE @Query OR ml.Town LIKE @Query OR ml.AdministrativeArea LIKE @Query)");
                parameters.Add("@Query", $"%{filter.Query}%");
            }

            return (where.Count > 0 ? $@"WHERE " + string.Join(" AND ", where) : "WHERE 1=1", parameters); // Ensure there's always a WHERE clause so that it can be appended to
        }
    }
}