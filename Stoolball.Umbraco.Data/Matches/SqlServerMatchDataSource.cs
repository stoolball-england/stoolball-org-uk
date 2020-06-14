using Dapper;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Teams;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
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
                    var matches = await connection.QueryAsync<Match, Tournament, TeamInMatch, Team, MatchLocation, Season, Competition, Match>(
                        $@"SELECT m.MatchId, m.MatchName, m.MatchType, m.StartTime, m.StartTimeIsKnown, m.MatchResultType, 
                            m.InningsOrderIsKnown, m.MatchNotes, m.MatchRoute, m.MemberKey,
                            tourney.TournamentRoute, tourney.TournamentName,
                            mt.TeamRole, mt.WonToss,
                            t.TeamId, t.TeamRoute, tn.TeamName, t.MemberGroupName,
                            ml.MatchLocationRoute, ml.SecondaryAddressableObjectName, ml.PrimaryAddressableObjectName, 
                            ml.Locality, ml.Town, ml.Latitude, ml.Longitude,
                            s.SeasonRoute, s.StartYear, s.EndYear,
                            co.CompetitionName, co.MemberGroupName
                            FROM {Tables.Match} AS m
                            LEFT JOIN {Tables.Tournament} AS tourney ON m.TournamentId = tourney.TournamentId
                            LEFT JOIN {Tables.MatchTeam} AS mt ON m.MatchId = mt.MatchId
                            LEFT JOIN {Tables.Team} AS t ON mt.TeamId = t.TeamId
                            LEFT JOIN {Tables.TeamName} AS tn ON t.TeamId = tn.TeamId AND tn.UntilDate IS NULL
                            LEFT JOIN {Tables.MatchLocation} AS ml ON m.MatchLocationId = ml.MatchLocationId
                            LEFT JOIN {Tables.Season} AS s ON m.SeasonId = s.SeasonId
                            LEFT JOIN {Tables.Competition} AS co ON s.CompetitionId = co.CompetitionId
                            WHERE LOWER(m.MatchRoute) = @Route",
                        (match, tournament, teamInMatch, team, matchLocation, season, competition) =>
                        {
                            match.Tournament = tournament;
                            if (teamInMatch != null && team != null)
                            {
                                teamInMatch.Team = team;
                                match.Teams.Add(teamInMatch);
                            }
                            match.MatchLocation = matchLocation;
                            if (season != null) { season.Competition = competition; }
                            match.Season = season;
                            return match;
                        },
                        new { Route = normalisedRoute },
                        splitOn: "TournamentRoute, TeamRole, TeamId, MatchLocationRoute, SeasonRoute, CompetitionName")
                        .ConfigureAwait(false);

                    var matchToReturn = matches.FirstOrDefault(); // get an example with the properties that are the same for every row
                    if (matchToReturn != null)
                    {
                        matchToReturn.Teams = matches.Select(match => match.Teams.SingleOrDefault()).OfType<TeamInMatch>().OrderBy(x => x.TeamRole).ToList();
                    }

                    if (matchToReturn != null)
                    {
                        var allInnings = await connection.QueryAsync<MatchInnings, Team, PlayerInnings, Over, MatchInnings>(
                            $@"SELECT i.Runs, i.Wickets, i.InningsOrderInMatch,
                               i.TeamId,
                               b.BattingPosition,
                               o.OverNumber
                               FROM {Tables.MatchInnings} i 
                               LEFT JOIN {Tables.PlayerInnings} b ON i.MatchInningsId = b.MatchInningsId
                               LEFT JOIN {Tables.Over} o ON i.MatchInningsId = o.MatchInningsId
                               WHERE i.MatchId = @MatchId
                               ORDER BY i.InningsOrderInMatch, b.BattingPosition, o.OverNumber",
                            (innings, team, batting, over) =>
                            {
                                if (team != null)
                                {
                                    innings.Team = team;
                                }
                                if (batting != null)
                                {
                                    innings.PlayerInnings.Add(batting);
                                }
                                if (over != null)
                                {
                                    innings.OversBowled.Add(over);
                                }
                                return innings;
                            },
                            new { matchToReturn.MatchId },
                            splitOn: "TeamId, BattingPosition, OverNumber")
                            .ConfigureAwait(false);

                        matchToReturn.MatchInnings = allInnings.OrderBy(x => x.InningsOrderInMatch).ToList();
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
