using Dapper;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Routing;
using System;
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
    }
}
