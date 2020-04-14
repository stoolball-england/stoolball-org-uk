using Dapper;
using Stoolball.Competitions;
using Stoolball.Routing;
using Stoolball.Teams;
using System;
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
        /// <returns>A matching <see cref="Season"/> or <c>null</c> if not found</returns>
        public async Task<Season> ReadSeasonByRoute(string route)
        {
            try
            {
                string normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "competitions", "^[0-9]{4}(-[0-9]{2})?$");

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    var seasons = await connection.QueryAsync<Season, Competition, Season, Team, Season>(
                        $@"SELECT s.StartYear, s.EndYear, s.Introduction, s.Results,
                            co.CompetitionName, co.Introduction, co.UntilDate, co.PublicContactDetails, co.Website,
                            s2.SeasonId, s2.StartYear, s2.EndYear, s2.SeasonRoute,
                            t.TeamId, tn.TeamName, t.TeamRoute
                            FROM {Tables.Season} AS s 
                            INNER JOIN {Tables.Competition} AS co ON co.CompetitionId = s.CompetitionId
                            LEFT JOIN {Tables.Season} AS s2 ON co.CompetitionId = s2.CompetitionId AND NOT s2.SeasonId = s.SeasonId
                            LEFT JOIN {Tables.SeasonTeam} AS st ON st.SeasonId = s.SeasonId
                            LEFT JOIN {Tables.Team} AS t ON t.TeamId = st.TeamId
                            LEFT JOIN {Tables.TeamName} AS tn ON t.TeamId = tn.TeamId AND tn.UntilDate IS NULL
                            WHERE LOWER(s.SeasonRoute) = @Route
                            ORDER BY s2.StartYear, s2.EndYear",
                        (season, competition, anotherSeasonInTheCompetition, team) =>
                        {
                            season.Competition = competition;
                            if (anotherSeasonInTheCompetition != null)
                            {
                                season.Competition.Seasons.Add(anotherSeasonInTheCompetition);
                            }
                            if (team != null)
                            {
                                season.Teams.Add(new TeamInSeason { Season = season, Team = team });
                            }
                            return season;
                        },
                        new { Route = normalisedRoute },
                        splitOn: "CompetitionName, SeasonId, TeamId").ConfigureAwait(false);

                    var seasonToReturn = seasons.FirstOrDefault(); // get an example with the properties that are the same for every row
                    if (seasonToReturn != null)
                    {
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
    }
}
