using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Routing;
using Stoolball.Schools;

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
                return await connection.ExecuteScalarAsync<int>($@"SELECT COUNT(sc.SchoolId)
                            FROM {Tables.School} AS sc
                            INNER JOIN {Tables.SchoolVersion} AS sv ON sc.SchoolId = sv.SchoolId
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

                var sql = $@"SELECT sc2.SchoolId, sv2.SchoolName, sc2.SchoolRoute
                            FROM {Tables.School} AS sc2
                            INNER JOIN {Tables.SchoolVersion} AS sv2 ON sc2.SchoolId = sv2.SchoolId
                            WHERE sc2.SchoolId IN(
                                SELECT sc.SchoolId
                                FROM {Tables.School} AS sc
                                INNER JOIN {Tables.SchoolVersion} AS sv ON sc.SchoolId = sv.SchoolId
                                {where}
                                AND sv.SchoolVersionId = (SELECT TOP 1 SchoolVersionId FROM {Tables.SchoolVersion} WHERE SchoolId = sc.SchoolId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                                ORDER BY CASE WHEN sv.UntilDate IS NULL THEN 0 ELSE 1 END, sv.ComparableName
                                OFFSET @PageOffset ROWS FETCH NEXT @PageSize ROWS ONLY
                            )
                            AND sv2.SchoolVersionId = (SELECT TOP 1 SchoolVersionId FROM {Tables.SchoolVersion} WHERE SchoolId = sc2.SchoolId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                            ORDER BY CASE WHEN sv2.UntilDate IS NULL THEN 0 ELSE 1 END, sv2.ComparableName";

                parameters.Add("@PageOffset", (filter.Paging.PageNumber - 1) * filter.Paging.PageSize);
                parameters.Add("@PageSize", filter.Paging.PageSize);

                var schools = await connection.QueryAsync<School, School, School>(sql,
                    (school, dummyForNow) =>
                    {
                        return school;
                    },
                    new DynamicParameters(parameters),
                    splitOn: "SchoolRoute").ConfigureAwait(false);

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
                where.Add("(sv.SchoolName LIKE @Query)");
                parameters.Add("@Query", $"%{filter.Query}%");
            }

            return (where.Count > 0 ? $@"WHERE " + string.Join(" AND ", where) : "WHERE 1=1", parameters); // Ensure there's always a WHERE clause so that it can be appended to
        }
    }
}