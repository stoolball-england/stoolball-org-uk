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
    /// Gets stoolball season data from the Umbraco database
    /// </summary>
    public class SqlServerSeasonDataSource : ISeasonDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly ILogger _logger;
        private readonly IRouteNormaliser _routeNormaliser;

        public SqlServerSeasonDataSource(IDatabaseConnectionFactory databaseConnectionFactory, ILogger logger, IRouteNormaliser routeNormaliser)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeNormaliser = routeNormaliser ?? throw new ArgumentNullException(nameof(routeNormaliser));
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
            try
            {
                string normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "competitions", @"^[a-z0-9-]+\/[0-9]{4}(-[0-9]{2})?$");

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    var seasons = await connection.QueryAsync<Season, Competition, string, Season>(
                        $@"SELECT s.SeasonId, s.FromYear, s.UntilYear, s.Results, s.SeasonRoute,
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
            catch (Exception ex)
            {
                _logger.Error(typeof(SqlServerSeasonDataSource), ex);
                throw;
            }
        }

        private async Task<Season> ReadSeasonWithRelatedDataByRoute(string route)
        {
            try
            {
                string normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "competitions", @"^[a-z0-9-]+\/[0-9]{4}(-[0-9]{2})?$");

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    var seasons = await connection.QueryAsync<Season, Competition, Season, TeamInSeason, Team, string, Season>(
                        $@"SELECT s.SeasonId, s.FromYear, s.UntilYear, s.Introduction, s.EnableTournaments, s.EnableResultsTable, s.ResultsTableIsLeagueTable, s.EnableRunsScored, s.EnableRunsConceded, s.Results, s.SeasonRoute,
                            co.CompetitionName, co.PlayerType, co.Introduction, co.UntilYear, co.PublicContactDetails, co.Website, co.CompetitionRoute, co.MemberGroupName,
                            s2.SeasonId, s2.FromYear, s2.UntilYear, s2.SeasonRoute,
                            st.WithdrawnDate,
                            t.TeamId, tn.TeamName, t.TeamRoute,
                            mt.MatchType
                            FROM {Tables.Season} AS s 
                            INNER JOIN {Tables.Competition} AS co ON co.CompetitionId = s.CompetitionId
                            LEFT JOIN {Tables.Season} AS s2 ON co.CompetitionId = s2.CompetitionId AND NOT s2.SeasonId = s.SeasonId
                            LEFT JOIN {Tables.SeasonTeam} AS st ON st.SeasonId = s.SeasonId
                            LEFT JOIN {Tables.Team} AS t ON t.TeamId = st.TeamId
                            LEFT JOIN {Tables.TeamName} AS tn ON t.TeamId = tn.TeamId AND tn.UntilDate IS NULL
                            LEFT JOIN {Tables.SeasonMatchType} AS mt ON s.SeasonId = mt.SeasonId
                            WHERE LOWER(s.SeasonRoute) = @Route
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
            catch (Exception ex)
            {
                _logger.Error(typeof(SqlServerSeasonDataSource), ex);
                throw;
            }
        }

        /// <summary>
        /// Reads the points rules that apply for a specific season
        /// </summary>
        public async Task<IEnumerable<PointsRule>> ReadPointsRules(Guid seasonId)
        {
            try
            {
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    return await connection.QueryAsync<PointsRule>(
                        $@"SELECT pr.MatchResultType, pr.HomePoints, pr.AwayPoints
                            FROM {Tables.SeasonPointsRule} AS pr 
                            WHERE pr.SeasonId = @SeasonId",
                        new { SeasonId = seasonId }).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(typeof(SqlServerSeasonDataSource), ex);
                throw;
            }
        }


        /// <summary>
        /// Reads the points adjustments that apply for a specific season
        /// </summary>
        public async Task<IEnumerable<PointsAdjustment>> ReadPointsAdjustments(Guid seasonId)
        {
            try
            {
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    return await connection.QueryAsync<PointsAdjustment, Team, PointsAdjustment>(
                        $@"SELECT spa.Points, spa.Reason, tn.TeamId, tn.TeamName
                            FROM {Tables.SeasonPointsAdjustment} AS spa 
                            INNER JOIN {Tables.TeamName} tn ON spa.TeamId = tn.TeamId AND tn.UntilDate IS NULL
                            WHERE spa.SeasonId = @SeasonId",
                        (pointsAdjustment, team) =>
                        {
                            pointsAdjustment.Team = team;
                            return pointsAdjustment;
                        },
                        new { SeasonId = seasonId },
                        splitOn: "TeamId").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(typeof(SqlServerSeasonDataSource), ex);
                throw;
            }
        }
    }
}
