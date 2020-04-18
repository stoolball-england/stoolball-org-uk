using Dapper;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Teams;
using System;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Umbraco.Data.Teams
{
    /// <summary>
    /// Gets stoolball team data from the Umbraco database
    /// </summary>
    public class SqlServerTeamDataSource : ITeamDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly ILogger _logger;
        private readonly IRouteNormaliser _routeNormaliser;

        public SqlServerTeamDataSource(IDatabaseConnectionFactory databaseConnectionFactory, ILogger logger, IRouteNormaliser routeNormaliser)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeNormaliser = routeNormaliser ?? throw new ArgumentNullException(nameof(routeNormaliser));
        }

        /// <summary>
        /// Gets a single team based on its route
        /// </summary>
        /// <param name="route">/teams/example-team</param>
        /// <returns>A matching <see cref="Team"/> or <c>null</c> if not found</returns>
        public async Task<Team> ReadTeamByRoute(string route)
        {
            try
            {
                string normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "teams");

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    var teams = await connection.QueryAsync<Team, Club, MatchLocation, Season, Competition, Team>(
                        $@"SELECT t.TeamId, tn.TeamName, t.TeamType, t.PlayerType, t.Introduction, t.AgeRangeLower, t.AgeRangeUpper,
                            t.Website, t.PublicContactDetails, t.PlayingTimes, t.Cost, t.TeamRoute, t.UntilDate,
                            cn.ClubName, c.ClubRoute, c.ClubMark,
                            ml.SecondaryAddressableObjectName, ml.PrimaryAddressableObjectName, ml.Locality, ml.Town, ml.AdministrativeArea, ml.MatchLocationRoute,
                            s.StartYear, s.EndYear, s.SeasonRoute,
                            co.CompetitionId, co.CompetitionName
                            FROM {Tables.Team} AS t 
                            INNER JOIN {Tables.TeamName} AS tn ON t.TeamId = tn.TeamId AND tn.UntilDate IS NULL
                            LEFT JOIN {Tables.Club} AS c ON t.ClubId = c.ClubId
                            LEFT JOIN {Tables.ClubName} AS cn ON c.ClubId = cn.ClubId AND cn.UntilDate IS NULL
                            LEFT JOIN {Tables.TeamMatchLocation} AS tml ON tml.TeamId = t.TeamId AND tml.UntilDate IS NULL
                            LEFT JOIN {Tables.MatchLocation} AS ml ON ml.MatchLocationId = tml.MatchLocationId 
                            LEFT JOIN {Tables.SeasonTeam} AS st ON t.TeamId = st.TeamId
                            LEFT JOIN {Tables.Season} AS s ON st.SeasonId = s.SeasonId
                            LEFT JOIN {Tables.Competition} AS co ON co.CompetitionId = s.CompetitionId
                            WHERE LOWER(t.TeamRoute) = @Route",
                        (team, club, matchLocation, season, competition) =>
                        {
                            team.Club = club;
                            if (matchLocation != null)
                            {
                                team.MatchLocations.Add(matchLocation);
                            }
                            if (season != null)
                            {
                                season.Competition = competition;
                                team.Seasons.Add(new TeamInSeason { Season = season });
                            }
                            return team;
                        },
                        new { Route = normalisedRoute },
                        splitOn: "ClubName, SecondaryAddressableObjectName, StartYear, CompetitionId").ConfigureAwait(false);

                    var teamToReturn = teams.FirstOrDefault(); // get an example with the properties that are the same for every row
                    if (teamToReturn != null)
                    {
                        teamToReturn.MatchLocations = teams.Select(team => team.MatchLocations.SingleOrDefault()).OfType<MatchLocation>().ToList();
                        teamToReturn.Seasons = teams.Select(team => team.Seasons.SingleOrDefault()).OfType<TeamInSeason>().ToList();
                    }

                    return teamToReturn;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(typeof(SqlServerTeamDataSource), ex);
                throw;
            }
        }
    }
}
