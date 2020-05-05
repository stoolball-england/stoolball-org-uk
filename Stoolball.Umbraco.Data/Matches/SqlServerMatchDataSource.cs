using Dapper;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Teams;
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
                    var sql = $@"SELECT m.MatchName, m.MatchType, m.PlayerType, m.TournamentQualificationType, 
                        m.StartTime, m.StartTimeIsKnown, m.SpacesInTournament, m.MatchRoute
                        FROM {Tables.Match} AS m
                        <<JOIN>>
                        <<WHERE>>
                        ORDER BY m.StartTime";

                    var join = new StringBuilder();
                    var where = new StringBuilder();
                    var parameters = new Dictionary<string, object>();

                    if (matchQuery?.MatchTypes?.Count > 0)
                    {
                        where.Append(where.Length > 0 ? "AND " : "WHERE ");
                        where.Append("m.MatchType IN @MatchTypes ");
                        parameters.Add("@MatchTypes", matchQuery.MatchTypes.Select(x => x.ToString()));
                    }

                    if (matchQuery?.ExcludeMatchTypes?.Count > 0)
                    {
                        where.Append(where.Length > 0 ? "AND " : "WHERE ");
                        where.Append("m.MatchType NOT IN @ExcludeMatchTypes ");
                        parameters.Add("@ExcludeMatchTypes", matchQuery.ExcludeMatchTypes.Select(x => x.ToString()));
                    }

                    if (matchQuery?.TeamIds?.Count > 0)
                    {
                        join.Append($"INNER JOIN {Tables.MatchTeam} mt ON m.MatchId = mt.MatchId ");

                        where.Append(where.Length > 0 ? "AND " : "WHERE ");
                        where.Append("mt.TeamId IN @TeamIds ");
                        parameters.Add("@TeamIds", matchQuery.TeamIds);
                    }

                    if (matchQuery?.SeasonIds?.Count > 0)
                    {
                        join.Append($"INNER JOIN {Tables.SeasonMatch} sm ON m.MatchId = sm.MatchId ");

                        where.Append(where.Length > 0 ? "AND " : "WHERE ");
                        where.Append("sm.SeasonId IN @SeasonIds ");
                        parameters.Add("@SeasonIds", matchQuery.SeasonIds);
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

        /// <summary>
        /// Gets a single stoolball match based on its route
        /// </summary>
        /// <param name="route">/matches/example-match</param>
        /// <returns>A matching <see cref="Match"/> or <c>null</c> if not found</returns>
        public async Task<Match> ReadMatchByRoute(string route)
        {
            try
            {
                string normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "matches");

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    var matches = await connection.QueryAsync<Match, Team, Match>(
                        $@"SELECT m.MatchName, m.StartTime,
                            t.TeamId, tn.TeamName, t.TeamRoute
                            FROM {Tables.Match} AS m
                            LEFT JOIN {Tables.MatchTeam} AS mt ON m.MatchId = mt.MatchId
                            LEFT JOIN {Tables.Team} AS t ON mt.TeamId = t.TeamId
                            LEFT JOIN {Tables.TeamName} AS tn ON t.TeamId = tn.TeamId AND tn.UntilDate IS NULL
                            WHERE LOWER(m.MatchRoute) = @Route
                            ORDER BY mt.TeamRole",
                        (match, team) =>
                        {
                            match.Teams.Add(new TeamInMatch { Team = team });
                            return match;
                        },
                        new { Route = normalisedRoute },
                        splitOn: "TeamId").ConfigureAwait(false);

                    var matchToReturn = matches.FirstOrDefault(); // get an example with the properties that are the same for every row
                    if (matchToReturn != null)
                    {
                        matchToReturn.Teams = matches.Select(match => match.Teams.SingleOrDefault()).OfType<TeamInMatch>().ToList();
                    }

                    return matchToReturn;
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
