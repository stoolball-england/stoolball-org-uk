using Dapper;
using Stoolball.Matches;
using Stoolball.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Umbraco.Data.Matches
{
    /// <summary>
    /// Gets stoolball match data from the Umbraco database
    /// </summary>
    public class SqlServerMatchDataSource : IMatchDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly ILogger _logger;
        private readonly IRouteNormaliser _routeNormaliser;

        public SqlServerMatchDataSource(IDatabaseConnectionFactory databaseConnectionFactory, ILogger logger, IRouteNormaliser routeNormaliser)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeNormaliser = routeNormaliser ?? throw new ArgumentNullException(nameof(routeNormaliser));
        }

        /// <summary>
        /// Gets a list of matches and tournaments based on a query
        /// </summary>
        /// <returns>A list of <see cref="MatchListing"/> objects. An empty list if no matches or tournaments are found.</returns>
        public async Task<List<MatchListing>> ReadMatchListings(MatchQuery matchQuery)
        {
            try
            {
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    var sql = $@"SELECT TOP 100 m.MatchName, m.MatchType, m.PlayerType, m.TournamentQualificationType, 
                        m.StartTime, m.StartTimeIsKnown, m.SpacesInTournament, m.MatchRoute
                        FROM {Tables.Match} AS m
                        <<JOIN>>
                        <<WHERE>>
                        ORDER BY m.StartTime";

                    var join = new StringBuilder();
                    var where = new StringBuilder();
                    var parameters = new Dictionary<string, object>();

                    if (matchQuery?.TeamIds?.Count > 0)
                    {
                        join.Append($"INNER JOIN {Tables.MatchTeam} mt ON m.MatchId = mt.MatchId ");

                        where.Append(where.Length > 0 ? "AND " : "WHERE ");
                        where.Append("mt.TeamId IN @TeamIds ");
                        parameters.Add("@TeamIds", matchQuery.TeamIds);
                    }

                    if (matchQuery?.FromDate != null)
                    {
                        where.Append(where.Length > 0 ? "AND " : "WHERE ");
                        where.Append("m.StartTime >= @FromDate");
                        parameters.Add("@FromDate", matchQuery.FromDate.Value);
                    }

                    sql = sql.Replace("<<JOIN>>", join.ToString())
                             .Replace("<<WHERE>>", where.ToString());

                    var matches = await connection.QueryAsync<MatchListing>(sql, new DynamicParameters(parameters)).ConfigureAwait(false);

                    return matches.Distinct(new MatchListingEqualityComparer()).ToList();
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
