using Dapper;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Teams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Umbraco.Data.Competitions
{
    /// <summary>
    /// Gets stoolball competition data from the Umbraco database
    /// </summary>
    public class SqlServerCompetitionDataSource : ICompetitionDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly ILogger _logger;
        private readonly IRouteNormaliser _routeNormaliser;

        public SqlServerCompetitionDataSource(IDatabaseConnectionFactory databaseConnectionFactory, ILogger logger, IRouteNormaliser routeNormaliser)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeNormaliser = routeNormaliser ?? throw new ArgumentNullException(nameof(routeNormaliser));
        }

        /// <summary>
        /// Gets a single stoolball competition based on its route, with the route of its latest season, if any
        /// </summary>
        /// <param name="route">/competitions/example-competition</param>
        /// <returns>A matching <see cref="Competition"/> or <c>null</c> if not found</returns>
        public async Task<Competition> ReadCompetitionByRoute(string route)
        {
            try
            {
                string normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "competitions");

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    var competitions = await connection.QueryAsync<Competition, Season, string, Competition>(
                        $@"SELECT co.CompetitionId, co.CompetitionName, co.PlayerType, co.Introduction, co.FromYear, co.UntilYear, co.Overs, co.PlayersPerTeam,
                            co.PublicContactDetails, co.PrivateContactDetails, co.Facebook, co.Twitter, co.Instagram, co.YouTube, co.Website, co.CompetitionRoute, co.MemberGroupName,
                            s.SeasonRoute, s.StartYear, s.EndYear,
                            mt.MatchType
                            FROM {Tables.Competition} AS co
                            LEFT JOIN {Tables.Season} AS s ON co.CompetitionId = s.CompetitionId
                            LEFT JOIN {Tables.SeasonMatchType} AS mt ON s.SeasonId = mt.SeasonId
                            WHERE LOWER(co.CompetitionRoute) = @Route
                            AND (s.StartYear = (SELECT MAX(StartYear) FROM {Tables.Season} WHERE CompetitionId = co.CompetitionId) 
                            OR s.StartYear IS NULL)",
                        (competition, season, matchType) =>
                        {
                            if (season != null)
                            {
                                if (!string.IsNullOrEmpty(matchType))
                                {
                                    season.MatchTypes.Add((MatchType)Enum.Parse(typeof(MatchType), matchType));
                                }
                                competition.Seasons.Add(season);
                            }
                            return competition;
                        },
                        new { Route = normalisedRoute },
                        splitOn: "SeasonRoute,MatchType").ConfigureAwait(false);

                    var competitionToReturn = competitions.FirstOrDefault(); // get an example with the properties that are the same for every row
                    if (competitionToReturn != null && competitionToReturn.Seasons.Count > 0)
                    {
                        competitionToReturn.Seasons[0].MatchTypes = competitions
                            .Select(competition => competition.Seasons.FirstOrDefault()?.MatchTypes.FirstOrDefault())
                            .OfType<MatchType>()
                            .Distinct()
                            .ToList();
                    }

                    return competitionToReturn;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(typeof(SqlServerCompetitionDataSource), ex);
                throw;
            }
        }

        /// <summary>
        /// Gets a list of competitions based on a query
        /// </summary>
        /// <returns>A list of <see cref="Competition"/> objects. An empty list if no competitions are found.</returns>
        public async Task<List<Competition>> ReadCompetitionListings(CompetitionQuery competitionQuery)
        {
            try
            {
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    var sql = $@"SELECT co.CompetitionId, co.CompetitionName, co.CompetitionRoute, co.UntilYear, co.PlayerType,
                            s.SeasonRoute,
                            st.TeamId
                            FROM {Tables.Competition} AS co 
                            LEFT JOIN {Tables.Season} AS s ON co.CompetitionId = s.CompetitionId
                            LEFT JOIN {Tables.SeasonTeam} AS st ON s.SeasonId = st.SeasonId
                            <<WHERE>>
                            (s.StartYear = (SELECT MAX(StartYear) FROM {Tables.Season} WHERE CompetitionId = co.CompetitionId) 
                            OR s.StartYear IS NULL)
                            ORDER BY CASE WHEN co.UntilYear IS NULL THEN 0 
                                          WHEN co.UntilYear IS NOT NULL THEN 1 END, co.CompetitionName";

                    var where = new List<string>();
                    var parameters = new Dictionary<string, object>();

                    if (!string.IsNullOrEmpty(competitionQuery?.Query))
                    {
                        where.Add("(co.CompetitionName LIKE @Query OR co.PlayerType LIKE @Query)");
                        parameters.Add("@Query", $"%{competitionQuery.Query}%");
                    }

                    sql = sql.Replace("<<WHERE>>", where.Count > 0 ? "WHERE " + string.Join(" AND ", where) + " AND " : "WHERE");

                    var competitions = await connection.QueryAsync<Competition, Season, Team, Competition>(sql,
                        (competition, season, team) =>
                        {
                            if (season != null)
                            {
                                competition.Seasons.Add(season);
                            }

                            if (team != null)
                            {
                                season.Teams.Add(new TeamInSeason { Team = team });
                            }
                            return competition;
                        },
                        new DynamicParameters(parameters),
                        splitOn: "SeasonRoute,TeamId").ConfigureAwait(false);

                    var resolvedCompetitions = competitions.GroupBy(competition => competition.CompetitionId).Select(copiesOfCompetition =>
                    {
                        var resolvedCompetition = copiesOfCompetition.First();
                        if (resolvedCompetition.Seasons.Count > 0)
                        {
                            resolvedCompetition.Seasons.First().Teams = copiesOfCompetition
                                        .Select(competition => competition.Seasons.SingleOrDefault()?.Teams.SingleOrDefault())
                                        .OfType<TeamInSeason>().ToList();
                        }
                        return resolvedCompetition;
                    }).ToList();

                    return resolvedCompetitions;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(typeof(SqlServerCompetitionDataSource), ex);
                throw;
            }
        }
    }
}
