using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Teams;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Gets stoolball competition data from the Umbraco database
    /// </summary>
    public class SqlServerCompetitionDataSource : ICompetitionDataSource, ICacheableCompetitionDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IRouteNormaliser _routeNormaliser;

        public SqlServerCompetitionDataSource(IDatabaseConnectionFactory databaseConnectionFactory, IRouteNormaliser routeNormaliser)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _routeNormaliser = routeNormaliser ?? throw new ArgumentNullException(nameof(routeNormaliser));
        }

        /// <summary>
        /// Gets a single stoolball competition based on its route, with basic details of its seasons, if any
        /// </summary>
        /// <param name="route">/competitions/example-competition</param>
        /// <returns>A matching <see cref="Competition"/> or <c>null</c> if not found</returns>
        public async Task<Competition?> ReadCompetitionByRoute(string route)
        {
            var normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "competitions");

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var competitions = await connection.QueryAsync<Competition, Season, OverSet, string, Competition>(
                    $@"SELECT co.CompetitionId, cv.CompetitionName, co.PlayerType, co.Introduction, YEAR(cv.UntilDate) AS UntilYear, 
                            co.PublicContactDetails, co.PrivateContactDetails, co.Facebook, co.Twitter, co.Instagram, co.YouTube, co.Website, co.CompetitionRoute, 
                            co.MemberGroupKey, co.MemberGroupName,
                            s.SeasonId, s.SeasonRoute, s.FromYear, s.UntilYear, s.PlayersPerTeam, s.EnableTournaments, s.EnableLastPlayerBatsOn, s.EnableBonusOrPenaltyRuns,
                            os.OverSetId, os.OverSetNumber, os.Overs, os.BallsPerOver,
                            mt.MatchType
                            FROM {Tables.Competition} AS co
                            INNER JOIN {Tables.CompetitionVersion} AS cv ON co.CompetitionId = cv.CompetitionId
                            LEFT JOIN {Tables.Season} AS s ON co.CompetitionId = s.CompetitionId
                            LEFT JOIN {Tables.OverSet} AS os ON s.SeasonId = os.SeasonId
                            LEFT JOIN {Tables.SeasonMatchType} AS mt ON s.SeasonId = mt.SeasonId
                            WHERE LOWER(co.CompetitionRoute) = @Route
                            AND cv.CompetitionVersionId = (SELECT TOP 1 CompetitionVersionId FROM {Tables.CompetitionVersion} WHERE CompetitionId = co.CompetitionId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                            ORDER BY s.FromYear DESC, s.UntilYear DESC",
                    (competition, season, overSet, matchType) =>
                    {
                        if (season != null)
                        {
                            if (!string.IsNullOrEmpty(matchType))
                            {
                                season.MatchTypes.Add((MatchType)Enum.Parse(typeof(MatchType), matchType));
                            }
                            if (overSet != null)
                            {
                                season.DefaultOverSets.Add(overSet);
                            }
                            competition.Seasons.Add(season);
                        }
                        return competition;
                    },
                    new { Route = normalisedRoute },
                    splitOn: "SeasonId,OverSetId,MatchType").ConfigureAwait(false);

                var competitionToReturn = competitions.FirstOrDefault(); // get an example with the properties that are the same for every row
                if (competitionToReturn != null && competitionToReturn.Seasons.Count > 0)
                {
                    competitionToReturn.Seasons = competitions.Select(competition => competition.Seasons.SingleOrDefault())
                                                    .OfType<Season>()
                                                    .Distinct(new SeasonEqualityComparer())
                                                    .ToList();

                    foreach (var season in competitionToReturn.Seasons)
                    {
                        season.MatchTypes = competitions
                            .SelectMany(competition => competition.Seasons.Where(x => x.SeasonId == season.SeasonId).SelectMany(x => x.MatchTypes))
                            .OfType<MatchType>()
                            .Distinct()
                            .ToList();
                        season.DefaultOverSets = competitions
                            .SelectMany(competition => competition.Seasons.Where(x => x.SeasonId == season.SeasonId).SelectMany(x => x.DefaultOverSets))
                            .OfType<OverSet>()
                            .Distinct(new OverSetEqualityComparer())
                            .OrderBy(x => x.OverSetNumber)
                            .ToList();
                    }
                }

                return competitionToReturn;
            }
        }

        /// <summary>
        /// Gets the number of competitions that match a query
        /// </summary>
        /// <returns></returns>
        public async Task<int> ReadTotalCompetitions(CompetitionFilter competitionQuery)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var (where, parameters) = BuildWhereClause(competitionQuery);
                return await connection.ExecuteScalarAsync<int>($@"SELECT COUNT(co.CompetitionId)
                            FROM {Tables.Competition} AS co
                            INNER JOIN {Tables.CompetitionVersion} AS cv ON co.CompetitionId = cv.CompetitionId
                            {where}
                            AND cv.CompetitionVersionId = (SELECT TOP 1 CompetitionVersionId FROM {Tables.CompetitionVersion} WHERE CompetitionId = co.CompetitionId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)",
                            new DynamicParameters(parameters)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets a list of competitions based on a query
        /// </summary>
        /// <returns>A list of <see cref="Competition"/> objects. An empty list if no competitions are found.</returns>
        public async Task<List<Competition>> ReadCompetitions(CompetitionFilter filter)
        {
            if (filter == null) { filter = new CompetitionFilter(); }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var (where, parameters) = BuildWhereClause(filter);

                var sql = $@"SELECT co2.CompetitionId, cv2.CompetitionName, co2.CompetitionRoute, YEAR(cv2.UntilDate) AS UntilYear, co2.PlayerType,
                            s2.SeasonId, s2.SeasonRoute,
                            st2.TeamId
                            FROM {Tables.Competition} AS co2
                            INNER JOIN {Tables.CompetitionVersion} AS cv2 ON co2.CompetitionId = cv2.CompetitionId
                            LEFT JOIN {Tables.Season} AS s2 ON co2.CompetitionId = s2.CompetitionId
                            LEFT JOIN {Tables.SeasonTeam} AS st2 ON s2.SeasonId = st2.SeasonId
                            WHERE co2.CompetitionId IN(
                                SELECT co.CompetitionId
                                FROM {Tables.Competition} AS co
                                INNER JOIN {Tables.CompetitionVersion} AS cv ON co.CompetitionId = cv.CompetitionId
                                LEFT JOIN {Tables.Season} AS s ON co.CompetitionId = s.CompetitionId
                                {where}
                                AND cv.CompetitionVersionId = (SELECT TOP 1 CompetitionVersionId FROM {Tables.CompetitionVersion} WHERE CompetitionId = co.CompetitionId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                                AND (s.FromYear = (SELECT MAX(FromYear) FROM {Tables.Season} WHERE CompetitionId = co.CompetitionId) OR s.FromYear IS NULL)
                                ORDER BY CASE WHEN cv.UntilDate IS NULL THEN 0 ELSE 1 END, s.UntilYear DESC, s.FromYear DESC, cv.ComparableName
                                OFFSET @PageOffset ROWS FETCH NEXT @PageSize ROWS ONLY
                            )
                            AND cv2.CompetitionVersionId = (SELECT TOP 1 CompetitionVersionId FROM {Tables.CompetitionVersion} WHERE CompetitionId = co2.CompetitionId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                            AND (s2.FromYear = (SELECT MAX(FromYear) FROM {Tables.Season} WHERE CompetitionId = co2.CompetitionId) OR s2.FromYear IS NULL)
                            ORDER BY CASE WHEN cv2.UntilDate IS NULL THEN 0 ELSE 1 END, s2.UntilYear DESC, s2.FromYear DESC, cv2.ComparableName";

                parameters.Add("@PageOffset", (filter.Paging.PageNumber - 1) * filter.Paging.PageSize);
                parameters.Add("@PageSize", filter.Paging.PageSize);

                var competitions = await connection.QueryAsync<Competition, Season, Team, Competition>(sql,
                    (competition, season, team) =>
                    {
                        if (season != null)
                        {
                            competition.Seasons.Add(season);
                            if (team != null)
                            {
                                season.Teams.Add(new TeamInSeason { Team = team });
                            }
                        }

                        return competition;
                    },
                    new DynamicParameters(parameters),
                    splitOn: "SeasonId,TeamId").ConfigureAwait(false);

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

        private static (string sql, Dictionary<string, object> parameters) BuildWhereClause(CompetitionFilter competitionQuery)
        {
            var where = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(competitionQuery?.Query))
            {
                where.Add("(cv.CompetitionName LIKE @Query OR co.PlayerType LIKE @Query)");
                parameters.Add("@Query", $"%{competitionQuery.Query}%");
            }

            return (where.Count > 0 ? $@"WHERE " + string.Join(" AND ", where) : "WHERE 1=1", parameters); // Ensure there's always a WHERE clause so that it can be appended to
        }
    }
}
