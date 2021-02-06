using System;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Teams;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Gets stoolball tournament data from the Umbraco database
    /// </summary>
    public class SqlServerTournamentDataSource : ITournamentDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IRouteNormaliser _routeNormaliser;

        public SqlServerTournamentDataSource(IDatabaseConnectionFactory databaseConnectionFactory, IRouteNormaliser routeNormaliser)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _routeNormaliser = routeNormaliser ?? throw new ArgumentNullException(nameof(routeNormaliser));
        }

        /// <summary>
        /// Gets a single tournament match based on its route
        /// </summary>
        /// <param name="route">/tournaments/example-tournament</param>
        /// <returns>A matching <see cref="Tournament"/> or <c>null</c> if not found</returns>
        public async Task<Tournament> ReadTournamentByRoute(string route)
        {
            var normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "tournaments");

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var sql = $@"SELECT tourney.TournamentId, tourney.TournamentName, tourney.PlayerType, tourney.StartTime, tourney.StartTimeIsKnown, 
                            tourney.PlayersPerTeam, tourney.QualificationType, tourney.MaximumTeamsInTournament, 
                            tourney.SpacesInTournament, tourney.TournamentNotes, tourney.TournamentRoute, tourney.MemberKey,
                            tt.TeamRole,
                            t.TeamId, t.TeamRoute, t.TeamType, tn.TeamName,
                            os.OverSetId, os.Overs, os.BallsPerOver,
                            ml.MatchLocationId, ml.MatchLocationRoute, ml.SecondaryAddressableObjectName, ml.PrimaryAddressableObjectName, 
                            ml.Locality, ml.Town, ml.Latitude, ml.Longitude,
                            s.SeasonId, s.SeasonRoute, s.FromYear, s.UntilYear,
                            cv.CompetitionName
                            FROM {Tables.Tournament} AS tourney
                            LEFT JOIN {Tables.TournamentTeam} AS tt ON tourney.TournamentId = tt.TournamentId
                            LEFT JOIN {Tables.Team} AS t ON tt.TeamId = t.TeamId
                            LEFT JOIN {Tables.TeamVersion} AS tn ON t.TeamId = tn.TeamId
                            LEFT JOIN {Tables.OverSet} os ON tourney.TournamentId = os.TournamentId
                            LEFT JOIN {Tables.MatchLocation} AS ml ON tourney.MatchLocationId = ml.MatchLocationId
                            LEFT JOIN {Tables.TournamentSeason} AS ts ON tourney.TournamentId = ts.TournamentId
                            LEFT JOIN {Tables.Season} AS s ON ts.SeasonId = s.SeasonId
                            LEFT JOIN {Tables.CompetitionVersion} AS cv ON s.CompetitionId = cv.CompetitionId
                            WHERE LOWER(tourney.TournamentRoute) = @Route
                            AND (tn.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = t.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC) OR tn.TeamVersionId IS NULL)
                            AND (cv.CompetitionVersionId = (SELECT TOP 1 CompetitionVersionId FROM {Tables.CompetitionVersion} WHERE CompetitionId = s.CompetitionId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC) OR cv.CompetitionVersionId IS NULL)
                            ORDER BY os.OverSetNumber";

                var tournaments = await connection.QueryAsync<Tournament, TeamInTournament, Team, OverSet, MatchLocation, Season, Competition, Tournament>(sql,
                    (tournament, teamInTournament, team, overSet, tournamentLocation, season, competition) =>
                    {
                        if (team != null)
                        {
                            teamInTournament.Team = team;
                            tournament.Teams.Add(teamInTournament);
                        }
                        if (overSet != null)
                        {
                            tournament.DefaultOverSets.Add(overSet);
                        }
                        tournament.TournamentLocation = tournamentLocation;
                        if (season != null) { season.Competition = competition; }
                        tournament.Seasons.Add(season);
                        return tournament;
                    },
                    new { Route = normalisedRoute },
                    splitOn: "TeamRole, TeamId, OverSetId, MatchLocationId, SeasonId, CompetitionName")
                    .ConfigureAwait(false);

                var tournamentToReturn = tournaments.FirstOrDefault(); // get an example with the properties that are the same for every row
                if (tournamentToReturn != null)
                {
                    tournamentToReturn.Teams = tournaments.Select(match => match.Teams.SingleOrDefault())
                        .OfType<TeamInTournament>()
                        .Distinct(new TeamInTournamentEqualityComparer())
                        .OrderBy(x => x.Team.ComparableName())
                        .ToList();
                    tournamentToReturn.DefaultOverSets = tournaments.Select(tournament => tournament.DefaultOverSets.SingleOrDefault())
                        .OfType<OverSet>()
                        .Distinct(new OverSetEqualityComparer())
                        .ToList();
                    tournamentToReturn.Seasons = tournaments.Select(tournament => tournament.Seasons.SingleOrDefault())
                        .OfType<Season>()
                        .Distinct(new SeasonEqualityComparer())
                        .OrderBy(x => x.Competition.ComparableName())
                        .ToList();
                }

                return tournamentToReturn;
            }
        }
    }
}
