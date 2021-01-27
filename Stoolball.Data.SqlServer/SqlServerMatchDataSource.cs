using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Awards;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Statistics;
using Stoolball.Teams;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Gets stoolball match data from the Umbraco database
    /// </summary>
    public class SqlServerMatchDataSource : IMatchDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IRouteNormaliser _routeNormaliser;

        public SqlServerMatchDataSource(IDatabaseConnectionFactory databaseConnectionFactory, IRouteNormaliser routeNormaliser)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _routeNormaliser = routeNormaliser ?? throw new ArgumentNullException(nameof(routeNormaliser));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Dapper uses it.")]
        private class MatchTeamIds
        {
            public Guid? BattingMatchTeamId { get; set; }
            public Guid? BowlingMatchTeamId { get; set; }
        }

        /// <summary>
        /// Gets a single stoolball match based on its route
        /// </summary>
        /// <param name="route">/matches/example-match</param>
        /// <returns>A matching <see cref="Match"/> or <c>null</c> if not found</returns>
        public async Task<Match> ReadMatchByRoute(string route)
        {
            var normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "matches");

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var matches = await connection.QueryAsync<Match, Tournament, TeamInMatch, Team, MatchLocation, Season, Competition, Match>(
                    $@"SELECT m.MatchId, m.MatchName, m.MatchType, m.StartTime, m.StartTimeIsKnown, m.MatchResultType, m.PlayersPerTeam,
                            m.LastPlayerBatsOn, m.EnableBonusOrPenaltyRuns, m.InningsOrderIsKnown, m.MatchNotes, m.MatchRoute, m.MemberKey, m.UpdateMatchNameAutomatically,
                            tourney.TournamentRoute, tourney.TournamentName, tourney.MemberKey,
                            mt.MatchTeamId, mt.TeamRole, mt.WonToss,
                            t.TeamId, t.TeamRoute, tn.TeamName, t.MemberGroupName,
                            ml.MatchLocationId, ml.MatchLocationRoute, ml.SecondaryAddressableObjectName, ml.PrimaryAddressableObjectName, 
                            ml.Locality, ml.Town, ml.Latitude, ml.Longitude,
                            s.SeasonId, s.SeasonRoute, s.FromYear, s.UntilYear,
                            co.CompetitionName, co.MemberGroupName, co.CompetitionRoute
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
                    splitOn: "TournamentRoute, MatchTeamId, TeamId, MatchLocationId, SeasonId, CompetitionName")
                    .ConfigureAwait(false);

                var matchToReturn = matches.FirstOrDefault(); // get an example with the properties that are the same for every row
                if (matchToReturn != null)
                {
                    matchToReturn.Teams = matches.Select(match => match.Teams.SingleOrDefault()).OfType<TeamInMatch>().OrderBy(x => x.TeamRole).ToList();
                }

                if (matchToReturn != null)
                {
                    // Add match innings and player innings within that to the match
                    var unprocessedInningsWithBatting = await connection.QueryAsync<MatchInnings, MatchTeamIds, PlayerInnings, PlayerIdentity, PlayerIdentity, PlayerIdentity, MatchInnings>(
                        $@"SELECT i.MatchInningsId, i.Overs, i.Byes, i.Wides, i.NoBalls, i.BonusOrPenaltyRuns, i.Runs, i.Wickets, i.InningsOrderInMatch,
                               i.BattingMatchTeamId, i.BowlingMatchTeamId,
                               pi.BattingPosition, pi.DismissalType, pi.RunsScored, pi.BallsFaced,
                               bat.PlayerIdentityId, bat.PlayerIdentityName, bat.TotalMatches, 
                               field.PlayerIdentityId, field.PlayerIdentityName, field.TotalMatches,
                               bowl.PlayerIdentityId, bowl.PlayerIdentityName, bowl.TotalMatches
                               FROM {Tables.MatchInnings} i 
                               LEFT JOIN {Tables.PlayerInnings} pi ON i.MatchInningsId = pi.MatchInningsId
                               LEFT JOIN {Tables.PlayerIdentity} bat ON pi.PlayerIdentityId = bat.PlayerIdentityId
                               LEFT JOIN {Tables.PlayerIdentity} field ON pi.DismissedById = field.PlayerIdentityId
                               LEFT JOIN {Tables.PlayerIdentity} bowl ON pi.BowlerId = bowl.PlayerIdentityId
                               WHERE i.MatchId = @MatchId
                               ORDER BY i.InningsOrderInMatch, pi.BattingPosition",
                        (innings, matchTeamIds, batting, batter, dismissedBy, bowledBy) =>
                        {
                            if (matchTeamIds != null && matchTeamIds.BattingMatchTeamId.HasValue)
                            {
                                innings.BattingTeam = matchToReturn.Teams.Single(x => x.MatchTeamId == matchTeamIds.BattingMatchTeamId);
                                innings.BattingMatchTeamId = matchTeamIds.BattingMatchTeamId;
                            }
                            if (matchTeamIds != null && matchTeamIds.BowlingMatchTeamId.HasValue)
                            {
                                innings.BowlingTeam = matchToReturn.Teams.Single(x => x.MatchTeamId == matchTeamIds.BowlingMatchTeamId);
                            }
                            if (batting != null)
                            {
                                batting.PlayerIdentity = batter;
                                batting.DismissedBy = dismissedBy;
                                batting.Bowler = bowledBy;
                                innings.PlayerInnings.Add(batting);
                            }
                            return innings;
                        },
                        new { matchToReturn.MatchId },
                        splitOn: "BattingMatchTeamId, BattingPosition, PlayerIdentityId, PlayerIdentityId, PlayerIdentityId")
                        .ConfigureAwait(false);

                    matchToReturn.MatchInnings = unprocessedInningsWithBatting.GroupBy(x => x.MatchInningsId).Select(inningsRows =>
                    {
                        var innings = inningsRows.First();
                        innings.PlayerInnings = inningsRows.Select(inningsRow => inningsRow.PlayerInnings.SingleOrDefault()).OfType<PlayerInnings>().ToList();
                        return innings;

                    }).OrderBy(x => x.InningsOrderInMatch).ToList();

                    // We now have the match innings. Get the overs recorded for them.
                    var unprocessedInningsWithOvers = await connection.QueryAsync<MatchInnings, Over, PlayerIdentity, Team, MatchInnings>(
                             $@"SELECT i.MatchInningsId,
                                    o.OverNumber, o.BallsBowled, o.NoBalls, o.Wides, o.RunsConceded,
                                    pi.PlayerIdentityId, pi.PlayerIdentityName, pi.TotalMatches, 
                                    pi.TeamId
                                    FROM {Tables.MatchInnings} i 
                                    INNER JOIN {Tables.Over} o ON i.MatchInningsId = o.MatchInningsId
                                    INNER JOIN {Tables.PlayerIdentity} pi ON o.PlayerIdentityId = pi.PlayerIdentityId
                                    WHERE i.MatchId = @MatchId
                                    ORDER BY i.InningsOrderInMatch, o.OverNumber",
                             (innings, over, bowler, team) =>
                             {
                                 over.PlayerIdentity = bowler;
                                 over.PlayerIdentity.Team = team;
                                 innings.OversBowled.Add(over);
                                 return innings;
                             },
                             new { matchToReturn.MatchId },
                             splitOn: "OverNumber, PlayerIdentityId, TeamId")
                             .ConfigureAwait(false);

                    // Add those overs to the existing instances of the match innings.
                    var processedInningsWithOvers = unprocessedInningsWithOvers.GroupBy(x => x.MatchInningsId).Select(inningsRows =>
                    {
                        var innings = inningsRows.First();
                        innings.OversBowled = inningsRows.Select(inningsRow => inningsRow.OversBowled.Single()).OfType<Over>().ToList();
                        return innings;
                    }).ToList();

                    foreach (var innings in matchToReturn.MatchInnings)
                    {
                        var overs = processedInningsWithOvers.SingleOrDefault(x => x.MatchInningsId == innings.MatchInningsId)?.OversBowled;
                        if (overs != null)
                        {
                            innings.OversBowled = overs;
                        }
                    }

                    // Add bowling figures
                    var bowlingFigures = await connection.QueryAsync<BowlingFigures, MatchInnings, PlayerIdentity, BowlingFigures>
                        ($@"SELECT bf.Overs, bf.Maidens, bf.RunsConceded, bf.Wickets,
                            bf.MatchInningsId,
                            pi.PlayerIdentityName
                            FROM {Tables.BowlingFigures} bf
                            INNER JOIN {Tables.PlayerIdentity} pi ON bf.PlayerIdentityId = pi.PlayerIdentityId
                            WHERE bf.MatchInningsId IN @MatchInningsIds
                            ORDER BY bf.MatchInningsId, bf.BowlingOrder",
                            (bowling, innings, bowler) =>
                            {
                                bowling.MatchInnings = innings;
                                bowling.Bowler = bowler;
                                return bowling;
                            },
                            new { MatchInningsIds = matchToReturn.MatchInnings.Select(x => x.MatchInningsId) },
                            splitOn: "MatchInningsId, PlayerIdentityName").ConfigureAwait(false);

                    foreach (var innings in matchToReturn.MatchInnings)
                    {
                        innings.BowlingFigures.AddRange(bowlingFigures.Where(x => x.MatchInnings.MatchInningsId == innings.MatchInningsId));
                    }

                    // Add awards - player of the match etc - to the match
                    matchToReturn.Awards = (await connection.QueryAsync<MatchAward, Award, PlayerIdentity, Team, MatchAward>(
                        $@"SELECT ma.AwardedToId, ma.Reason, a.AwardName, p.PlayerIdentityId, p.PlayerIdentityName, p.TotalMatches, p.TeamId
                               FROM {Tables.AwardedTo} ma
                               INNER JOIN {Tables.Award} a ON ma.AwardId = a.AwardId
                               INNER JOIN {Tables.PlayerIdentity} p ON ma.PlayerIdentityId = p.PlayerIdentityId
                               WHERE ma.MatchId = @MatchId
                               ORDER BY a.AwardName",
                        (matchAward, award, playerIdentity, team) =>
                        {
                            matchAward.Award = award;
                            matchAward.PlayerIdentity = playerIdentity;
                            matchAward.PlayerIdentity.Team = team;
                            return matchAward;
                        },
                        new { matchToReturn.MatchId },
                        splitOn: "AwardName, PlayerIdentityId, TeamId").ConfigureAwait(false)).ToList();
                }

                return matchToReturn;
            }
        }
    }
}
