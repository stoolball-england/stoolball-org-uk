using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Teams;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Gets stoolball season data from the Umbraco database
    /// </summary>
    public class SqlServerSeasonDataSource : ISeasonDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IRouteNormaliser _routeNormaliser;

        public SqlServerSeasonDataSource(IDatabaseConnectionFactory databaseConnectionFactory, IRouteNormaliser routeNormaliser)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _routeNormaliser = routeNormaliser ?? throw new ArgumentNullException(nameof(routeNormaliser));
        }

        /// <summary>
        /// Gets a list of seasons based on a query
        /// </summary>
        /// <returns>A list of <see cref="Season"/> objects. An empty list if no seasons are found.</returns>
        public async Task<List<Season>> ReadSeasons(CompetitionFilter? filter)
        {
            if (filter is null)
            {
                filter = new CompetitionFilter();
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var (where, parameters) = BuildWhereClause(filter);

                var sql = $@"SELECT s2.SeasonId, s2.FromYear, s2.UntilYear,
                            cv2.CompetitionName, YEAR(cv2.UntilDate) AS UntilYear
                            FROM {Tables.Season} AS s2 
                            INNER JOIN {Tables.Competition} AS co2 ON co2.CompetitionId = s2.CompetitionId
                            INNER JOIN {Tables.CompetitionVersion} AS cv2 ON cv2.CompetitionId = s2.CompetitionId
                            WHERE s2.SeasonId IN(
                                SELECT s.SeasonId
                                FROM {Tables.Season} AS s
                                INNER JOIN {Tables.Competition} AS co ON co.CompetitionId = s.CompetitionId
                                INNER JOIN {Tables.CompetitionVersion} cv ON co.CompetitionId = cv.CompetitionId
                                {where}
                                AND cv.CompetitionVersionId = (SELECT TOP 1 CompetitionVersionId FROM {Tables.CompetitionVersion} WHERE CompetitionId = co.CompetitionId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                                ORDER BY CASE WHEN cv.UntilDate IS NULL THEN 0
                                          WHEN cv.UntilDate IS NOT NULL THEN 1 END, s.FromYear DESC, s.UntilYear DESC, cv.ComparableName
                                OFFSET @PageOffset ROWS FETCH NEXT @PageSize ROWS ONLY
                            )
                            AND cv2.CompetitionVersionId = (SELECT TOP 1 CompetitionVersionId FROM {Tables.CompetitionVersion} WHERE CompetitionId = s2.CompetitionId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                            ORDER BY CASE WHEN cv2.UntilDate IS NULL THEN 0
                                          WHEN cv2.UntilDate IS NOT NULL THEN 1 END, s2.FromYear DESC, s2.UntilYear DESC, cv2.ComparableName";

                parameters.Add("@PageOffset", (filter.Paging.PageNumber - 1) * filter.Paging.PageSize);
                parameters.Add("@PageSize", filter.Paging.PageSize);

                var seasons = await connection.QueryAsync<Season, Competition, Season>(sql,
                    (season, competition) =>
                    {
                        season.Competition = competition;
                        return season;
                    },
                    new DynamicParameters(parameters),
                    splitOn: "CompetitionName").ConfigureAwait(false);

                return seasons.ToList();
            }
        }

        private static (string sql, Dictionary<string, object> parameters) BuildWhereClause(CompetitionFilter filter)
        {
            var where = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(filter.Query))
            {
                where.Add("(CONCAT(CompetitionName, ', ', s.FromYear, ' season') LIKE @Query OR CONCAT(CompetitionName, ', ', s.FromYear, '/', RIGHT(s.UntilYear,2), ' season') LIKE @Query OR co.PlayerType LIKE @Query)");
                parameters.Add("@Query", $"%{filter.Query}%");
            }

            if (filter.PlayerTypes?.Count > 0)
            {
                where.Add("co.PlayerType IN @PlayerTypes");
                parameters.Add("@PlayerTypes", filter.PlayerTypes.Select(x => x.ToString()));
            }

            if (filter.MatchTypes?.Count > 0)
            {
                where.Add($"s.SeasonId IN (SELECT SeasonId FROM {Tables.SeasonMatchType} WHERE MatchType IN @MatchTypes)");
                parameters.Add("@MatchTypes", filter.MatchTypes.Select(x => x.ToString()));
            }

            if (filter.FromYear.HasValue)
            {
                where.Add("s.FromYear = @FromYear");
                parameters.Add("@FromYear", filter.FromYear.Value);
            }

            if (filter.UntilYear.HasValue)
            {
                where.Add("s.UntilYear = @UntilYear");
                parameters.Add("@UntilYear", filter.UntilYear.Value);
            }

            if (filter.EnableTournaments.HasValue)
            {
                where.Add("s.EnableTournaments = @EnableTournaments");
                parameters.Add("@EnableTournaments", filter.EnableTournaments.Value);
            }

            return (where.Count > 0 ? $@"WHERE " + string.Join(" AND ", where) : "WHERE 1=1", parameters); // There must always be a WHERE clause so it can be appended to
        }

        /// <summary>
        /// Gets a single stoolball season based on its route
        /// </summary>
        /// <param name="route">/competitions/example-competition/2020</param>
        /// <param name="includeRelated"><c>true</c> to include the teams and other seasons in the competition; <c>false</c> otherwise</param>
        /// <returns>A matching <see cref="Season"/> or <c>null</c> if not found</returns>
        public async Task<Season?> ReadSeasonByRoute(string route, bool includeRelated = false)
        {
            var normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "competitions", @"^[a-z0-9-]+\/[0-9]{4}(-[0-9]{2})?$");

            return await (includeRelated ?
                ReadSeasonWithRelatedData("LOWER(s.SeasonRoute) = @Route", new { Route = normalisedRoute }) :
                ReadSeason("LOWER(s.SeasonRoute) = @Route", new { Route = normalisedRoute })).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a single stoolball season based on its id
        /// </summary>
        /// <returns>A matching <see cref="Season"/> or <c>null</c> if not found</returns>
        public async Task<Season?> ReadSeasonById(Guid seasonId, bool includeRelated = false)
        {
            return await (includeRelated ? ReadSeasonWithRelatedData("s.SeasonId = @seasonId", new { seasonId }) :
                ReadSeason("s.SeasonId = @seasonId", new { seasonId })).ConfigureAwait(false);
        }

        private async Task<Season?> ReadSeason(string where, object parameters)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var seasons = await connection.QueryAsync<Season, Competition, string, Season>(
                    $@"SELECT s.SeasonId, s.FromYear, s.UntilYear, s.Results, s.SeasonRoute, s.EnableTournaments, s.PlayersPerTeam, 
                            cv.CompetitionName, co.PlayerType, YEAR(cv.UntilDate) AS UntilYear, co.CompetitionRoute, co.MemberGroupName,
                            smt.MatchType
                            FROM {Tables.Season} AS s 
                            INNER JOIN {Tables.Competition} AS co ON co.CompetitionId = s.CompetitionId
                            INNER JOIN {Tables.CompetitionVersion} AS cv ON co.CompetitionId = cv.CompetitionId
                            LEFT JOIN {Tables.SeasonMatchType} AS smt ON s.SeasonId = smt.SeasonId
                            WHERE {where}
                            AND cv.CompetitionVersionId = (SELECT TOP 1 CompetitionVersionId FROM {Tables.CompetitionVersion} WHERE CompetitionId = co.CompetitionId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)",
                    (season, competition, matchType) =>
                    {
                        season.Competition = competition;
                        if (!string.IsNullOrEmpty(matchType))
                        {
                            season.MatchTypes.Add((MatchType)Enum.Parse(typeof(MatchType), matchType));
                        }
                        return season;
                    },
                    parameters,
                    splitOn: "CompetitionName, MatchType").ConfigureAwait(false);


                var seasonToReturn = seasons.FirstOrDefault(); // get an example with the properties that are the same for every row
                if (seasonToReturn != null)
                {
                    seasonToReturn.MatchTypes = seasons
                        .Where(season => season.MatchTypes.Any())
                        .Select(season => season.MatchTypes.First())
                        .OfType<MatchType>()
                        .Distinct()
                        .ToList();
                }

                return seasonToReturn;
            }
        }

        private async Task<Season?> ReadSeasonWithRelatedData(string where, object parameters)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var seasons = (await connection.QueryAsync<Season, Competition, Season, TeamInSeason, Team, OverSet, string, Season?>(
                    $@"SELECT s.SeasonId, s.FromYear, s.UntilYear, s.Introduction, s.PlayersPerTeam, s.EnableTournaments, s.ResultsTableType, 
                            s.EnableLastPlayerBatsOn, s.EnableBonusOrPenaltyRuns, s.EnableRunsScored, s.EnableRunsConceded, s.Results, s.SeasonRoute,
                            cv.CompetitionName, co.PlayerType, co.Introduction, YEAR(cv.UntilDate) AS UntilYear, co.PublicContactDetails, co.Website, 
                            co.Facebook, co.Twitter, co.Instagram, co.YouTube, co.CompetitionRoute, co.MemberGroupName,
                            s2.SeasonId, s2.FromYear, s2.UntilYear, s2.SeasonRoute,
                            st.WithdrawnDate,
                            t.TeamId, tn.TeamName, t.TeamRoute,
                            os.OverSetId, os.OverSetNumber, os.Overs, os.BallsPerOver,
                            mt.MatchType
                            FROM {Tables.Season} AS s 
                            INNER JOIN {Tables.Competition} AS co ON co.CompetitionId = s.CompetitionId
                            INNER JOIN {Tables.CompetitionVersion} cv ON co.CompetitionId = cv.CompetitionId
                            LEFT JOIN {Tables.Season} AS s2 ON co.CompetitionId = s2.CompetitionId AND NOT s2.SeasonId = s.SeasonId
                            LEFT JOIN {Tables.SeasonTeam} AS st ON st.SeasonId = s.SeasonId
                            LEFT JOIN {Tables.Team} AS t ON t.TeamId = st.TeamId
                            LEFT JOIN {Tables.TeamVersion} AS tn ON t.TeamId = tn.TeamId
                            LEFT JOIN {Tables.OverSet} os ON s.SeasonId = os.SeasonId
                            LEFT JOIN {Tables.SeasonMatchType} AS mt ON s.SeasonId = mt.SeasonId
                            WHERE {where}
                            AND (tn.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = t.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC) OR tn.TeamVersionId IS NULL)
                            AND cv.CompetitionVersionId = (SELECT TOP 1 CompetitionVersionId FROM {Tables.CompetitionVersion} WHERE CompetitionId = co.CompetitionId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                            ORDER BY s2.FromYear DESC, s2.UntilYear ASC",
                    (season, competition, anotherSeasonInTheCompetition, teamInSeason, team, overSet, matchType) =>
                    {
                        if (season != null)
                        {
                            season.Competition = competition;
                            if (!string.IsNullOrEmpty(matchType))
                            {
                                season.MatchTypes.Add((MatchType)Enum.Parse(typeof(MatchType), matchType));
                            }
                            if (anotherSeasonInTheCompetition != null)
                            {
                                season.Competition.Seasons.Add(anotherSeasonInTheCompetition);
                            }
                            if (team != null)
                            {
                                season.Teams.Add(new TeamInSeason
                                {
                                    Season = season,
                                    Team = team,
                                    WithdrawnDate = teamInSeason?.WithdrawnDate
                                });
                            }
                            if (overSet != null)
                            {
                                season.DefaultOverSets.Add(overSet);
                            }
                        }
                        return season;
                    },
                    parameters,
                    splitOn: "CompetitionName, SeasonId, WithdrawnDate, TeamId, OverSetId, MatchType").ConfigureAwait(false)).OfType<Season>();

                var seasonToReturn = seasons.FirstOrDefault(); // get an example with the properties that are the same for every row
                if (seasonToReturn != null)
                {
                    seasonToReturn.MatchTypes = seasons
                        .Where(season => season.MatchTypes.Any())
                        .Select(season => season.MatchTypes.First())
                        .OfType<MatchType>()
                        .Distinct()
                        .ToList();
                    seasonToReturn.Competition.Seasons = seasons.Select(season => season.Competition.Seasons.SingleOrDefault())
                        .GroupBy(season => season?.SeasonId)
                        .Select(duplicateSeasons => duplicateSeasons.First())
                        .OfType<Season>()
                        .ToList();
                    seasonToReturn.Teams = seasons
                        .Select(season => season.Teams.SingleOrDefault())
                        .GroupBy(teamInSeason => teamInSeason?.Team.TeamId)
                        .Select(duplicateTeamInSeason => duplicateTeamInSeason.First())
                        .OfType<TeamInSeason>()
                        .OrderBy(team => team.Team.ComparableName())
                        .ToList();
                    seasonToReturn.DefaultOverSets = seasons
                        .Select(season => season.DefaultOverSets.FirstOrDefault())
                        .OfType<OverSet>()
                        .Distinct(new OverSetEqualityComparer())
                        .OrderBy(overSet => overSet.OverSetNumber)
                        .ToList();
                }

                return seasonToReturn;
            }
        }

        /// <summary>
        /// Reads the points rules that apply for a specific season
        /// </summary>
        public async Task<IEnumerable<PointsRule>> ReadPointsRules(Guid seasonId)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.QueryAsync<PointsRule>(
                    $@"SELECT pr.PointsRuleId, pr.MatchResultType, pr.HomePoints, pr.AwayPoints
                            FROM {Tables.PointsRule} AS pr 
                            WHERE pr.SeasonId = @SeasonId",
                    new { SeasonId = seasonId }).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Reads the points adjustments that apply for a specific season
        /// </summary>
        public async Task<IEnumerable<PointsAdjustment>> ReadPointsAdjustments(Guid seasonId)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.QueryAsync<PointsAdjustment, Team, PointsAdjustment>(
                    $@"SELECT spa.PointsAdjustmentId, spa.Points, spa.Reason, tn.TeamId, tn.TeamName
                            FROM {Tables.PointsAdjustment} AS spa 
                            INNER JOIN {Tables.TeamVersion} tn ON spa.TeamId = tn.TeamId
                            WHERE spa.SeasonId = @SeasonId
                            AND tn.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = spa.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)",
                    (pointsAdjustment, team) =>
                    {
                        pointsAdjustment.Team = team;
                        return pointsAdjustment;
                    },
                    new { SeasonId = seasonId },
                    splitOn: "TeamId").ConfigureAwait(false);
            }
        }
    }
}
