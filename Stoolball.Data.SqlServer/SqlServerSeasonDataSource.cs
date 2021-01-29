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
using static Stoolball.Constants;

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
        public async Task<List<Season>> ReadSeasons(CompetitionQuery competitionQuery)
        {
            if (competitionQuery is null)
            {
                throw new ArgumentNullException(nameof(competitionQuery));
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var (where, parameters) = BuildWhereClause(competitionQuery);

                //LEFT JOIN {Tables.SeasonTeam} AS st2 ON s2.SeasonId = st2.SeasonId
                var sql = $@"SELECT s2.SeasonId, s2.FromYear, s2.UntilYear,
                            co2.CompetitionName
                            FROM {Tables.Season} AS s2 
                            INNER JOIN {Tables.Competition} AS co2 ON co2.CompetitionId = s2.CompetitionId
                            WHERE s2.SeasonId IN(
                                SELECT s.SeasonId
                                FROM {Tables.Season} AS s
                                INNER JOIN {Tables.Competition} AS co ON co.CompetitionId = s.CompetitionId
                                {where}
                                ORDER BY CASE WHEN co.UntilYear IS NULL THEN 0
                                          WHEN co.UntilYear IS NOT NULL THEN 1 END, s.FromYear DESC, s.UntilYear DESC, co.CompetitionName
                                OFFSET {(competitionQuery.PageNumber - 1) * competitionQuery.PageSize} ROWS FETCH NEXT {competitionQuery.PageSize} ROWS ONLY
                            )
                            ORDER BY CASE WHEN co2.UntilYear IS NULL THEN 0
                                          WHEN co2.UntilYear IS NOT NULL THEN 1 END, s2.FromYear DESC, s2.UntilYear DESC, co2.CompetitionName";

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

        private static (string sql, Dictionary<string, object> parameters) BuildWhereClause(CompetitionQuery competitionQuery)
        {
            var where = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(competitionQuery.Query))
            {
                where.Add("(CONCAT(CompetitionName, ', ', s.FromYear, ' season') LIKE @Query OR CONCAT(CompetitionName, ', ', s.FromYear, '/', RIGHT(s.UntilYear,2), ' season') LIKE @Query OR co.PlayerType LIKE @Query)");
                parameters.Add("@Query", $"%{competitionQuery.Query}%");
            }

            if (competitionQuery.MatchTypes?.Count > 0)
            {
                where.Add($"s.SeasonId IN (SELECT SeasonId FROM {Tables.SeasonMatchType} WHERE MatchType IN @MatchTypes)");
                parameters.Add("@MatchTypes", competitionQuery.MatchTypes.Select(x => x.ToString()));
            }

            return (where.Count > 0 ? $@"WHERE " + string.Join(" AND ", where) : string.Empty, parameters);
        }

        /// <summary>
        /// Gets a single stoolball season based on its route
        /// </summary>
        /// <param name="route">/competitions/example-competition/2020</param>
        /// <param name="includeRelated"><c>true</c> to include the teams and other seasons in the competition; <c>false</c> otherwise</param>
        /// <returns>A matching <see cref="Season"/> or <c>null</c> if not found</returns>
        public async Task<Season> ReadSeasonByRoute(string route, bool includeRelated = false)
        {
            return await (includeRelated ? ReadSeasonWithRelatedDataByRoute(route) : ReadSeasonByRoute(route)).ConfigureAwait(false);
        }

        private async Task<Season> ReadSeasonByRoute(string route)
        {
            var normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "competitions", @"^[a-z0-9-]+\/[0-9]{4}(-[0-9]{2})?$");

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var seasons = await connection.QueryAsync<Season, Competition, string, Season>(
                    $@"SELECT s.SeasonId, s.FromYear, s.UntilYear, s.Results, s.SeasonRoute, s.EnableTournaments, s.PlayersPerTeam, s.Overs,
                            co.CompetitionName, co.PlayerType, co.UntilYear, co.CompetitionRoute, co.MemberGroupName,
                            smt.MatchType
                            FROM {Tables.Season} AS s 
                            INNER JOIN {Tables.Competition} AS co ON co.CompetitionId = s.CompetitionId
                            LEFT JOIN {Tables.SeasonMatchType} AS smt ON s.SeasonId = smt.SeasonId
                            WHERE LOWER(s.SeasonRoute) = @Route",
                    (season, competition, matchType) =>
                    {
                        season.Competition = competition;
                        if (!string.IsNullOrEmpty(matchType))
                        {
                            season.MatchTypes.Add((MatchType)Enum.Parse(typeof(MatchType), matchType));
                        }
                        return season;
                    },
                    new { Route = normalisedRoute },
                    splitOn: "CompetitionName, MatchType").ConfigureAwait(false);


                var seasonToReturn = seasons.FirstOrDefault(); // get an example with the properties that are the same for every row
                if (seasonToReturn != null)
                {
                    seasonToReturn.MatchTypes = seasons
                        .Select(season => season.MatchTypes.FirstOrDefault())
                        .OfType<MatchType>()
                        .Distinct()
                        .ToList();
                }

                return seasonToReturn;
            }
        }

        private async Task<Season> ReadSeasonWithRelatedDataByRoute(string route)
        {
            var normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "competitions", @"^[a-z0-9-]+\/[0-9]{4}(-[0-9]{2})?$");

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var seasons = await connection.QueryAsync<Season, Competition, Season, TeamInSeason, Team, string, Season>(
                    $@"SELECT s.SeasonId, s.FromYear, s.UntilYear, s.Introduction, s.PlayersPerTeam, s.Overs, s.EnableTournaments, s.ResultsTableType, 
                            s.EnableLastPlayerBatsOn, s.EnableBonusOrPenaltyRuns, s.EnableRunsScored, s.EnableRunsConceded, s.Results, s.SeasonRoute,
                            co.CompetitionName, co.PlayerType, co.Introduction, co.UntilYear, co.PublicContactDetails, co.Website, 
                            co.Facebook, co.Twitter, co.Instagram, co.YouTube, co.CompetitionRoute, co.MemberGroupName,
                            s2.SeasonId, s2.FromYear, s2.UntilYear, s2.SeasonRoute,
                            st.WithdrawnDate,
                            t.TeamId, tn.TeamName, t.TeamRoute,
                            mt.MatchType
                            FROM {Tables.Season} AS s 
                            INNER JOIN {Tables.Competition} AS co ON co.CompetitionId = s.CompetitionId
                            LEFT JOIN {Tables.Season} AS s2 ON co.CompetitionId = s2.CompetitionId AND NOT s2.SeasonId = s.SeasonId
                            LEFT JOIN {Tables.SeasonTeam} AS st ON st.SeasonId = s.SeasonId
                            LEFT JOIN {Tables.Team} AS t ON t.TeamId = st.TeamId
                            LEFT JOIN {Tables.TeamVersion} AS tn ON t.TeamId = tn.TeamId
                            LEFT JOIN {Tables.SeasonMatchType} AS mt ON s.SeasonId = mt.SeasonId
                            WHERE LOWER(s.SeasonRoute) = @Route
                            AND tn.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = t.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                            ORDER BY s2.FromYear DESC, s2.UntilYear ASC",
                    (season, competition, anotherSeasonInTheCompetition, teamInSeason, team, matchType) =>
                    {
                        if (season != null)
                        {
                            season.Competition = competition;
                            if (!string.IsNullOrEmpty(matchType))
                            {
                                season.MatchTypes.Add((MatchType)Enum.Parse(typeof(MatchType), matchType));
                            }
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
                        return season;
                    },
                    new { Route = normalisedRoute },
                    splitOn: "CompetitionName, SeasonId, WithdrawnDate, TeamId, MatchType").ConfigureAwait(false);

                var seasonToReturn = seasons.FirstOrDefault(); // get an example with the properties that are the same for every row
                if (seasonToReturn != null)
                {
                    seasonToReturn.MatchTypes = seasons
                        .Select(season => season.MatchTypes.FirstOrDefault())
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
                        .OrderBy(team => team.Team.TeamName)
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
                    $@"SELECT pr.SeasonPointsRuleId AS PointsRuleId, pr.MatchResultType, pr.HomePoints, pr.AwayPoints
                            FROM {Tables.SeasonPointsRule} AS pr 
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
                    $@"SELECT spa.Points, spa.Reason, tn.TeamId, tn.TeamName
                            FROM {Tables.SeasonPointsAdjustment} AS spa 
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
