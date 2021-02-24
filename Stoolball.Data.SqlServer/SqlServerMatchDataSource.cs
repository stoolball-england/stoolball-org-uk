using System;
using System.Data.SqlTypes;
using System.Globalization;
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Dapper uses it.")]
        private class PlayerDto
        {
            public Guid PlayerId { get; set; }
            public Guid PlayerIdentityId { get; set; }
            public string PlayerIdentityName { get; set; }
            public string PlayerRoute { get; set; }
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
                    $@"SELECT m.MatchId, m.MatchName, m.MatchType, m.PlayerType, m.StartTime, m.StartTimeIsKnown, m.MatchResultType, m.PlayersPerTeam,
                            m.LastPlayerBatsOn, m.EnableBonusOrPenaltyRuns, m.InningsOrderIsKnown, m.MatchNotes, m.MatchRoute, m.MemberKey, m.UpdateMatchNameAutomatically,
                            tourney.TournamentId, tourney.TournamentRoute, tourney.TournamentName, tourney.MemberKey,
                            mt.MatchTeamId, mt.TeamRole, mt.WonToss,
                            t.TeamId, t.TeamRoute, tn.TeamName, t.MemberGroupName,
                            ml.MatchLocationId, ml.MatchLocationRoute, ml.SecondaryAddressableObjectName, ml.PrimaryAddressableObjectName, 
                            ml.Locality, ml.Town, ml.Latitude, ml.Longitude,
                            s.SeasonId, s.SeasonRoute, s.FromYear, s.UntilYear,
                            co.CompetitionId, cv.CompetitionName, co.MemberGroupName, co.CompetitionRoute
                            FROM {Tables.Match} AS m
                            LEFT JOIN {Tables.Tournament} AS tourney ON m.TournamentId = tourney.TournamentId
                            LEFT JOIN {Tables.MatchTeam} AS mt ON m.MatchId = mt.MatchId
                            LEFT JOIN {Tables.Team} AS t ON mt.TeamId = t.TeamId
                            LEFT JOIN {Tables.TeamVersion} AS tn ON t.TeamId = tn.TeamId
                            LEFT JOIN {Tables.MatchLocation} AS ml ON m.MatchLocationId = ml.MatchLocationId
                            LEFT JOIN {Tables.Season} AS s ON m.SeasonId = s.SeasonId
                            LEFT JOIN {Tables.Competition} AS co ON s.CompetitionId = co.CompetitionId
                            LEFT JOIN {Tables.CompetitionVersion} AS cv ON co.CompetitionId = cv.CompetitionId
                            WHERE LOWER(m.MatchRoute) = @Route
                            AND (tn.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = t.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC) OR tn.TeamVersionId IS NULL)
                            AND (cv.CompetitionVersionId = (SELECT TOP 1 CompetitionVersionId FROM {Tables.CompetitionVersion} WHERE CompetitionId = co.CompetitionId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC) OR cv.CompetitionVersionId IS NULL)",
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
                    splitOn: "TournamentId, MatchTeamId, TeamId, MatchLocationId, SeasonId, CompetitionId")
                    .ConfigureAwait(false);

                var matchToReturn = matches.FirstOrDefault(); // get an example with the properties that are the same for every row
                if (matchToReturn != null)
                {
                    matchToReturn.Teams = matches.Select(match => match.Teams.SingleOrDefault()).OfType<TeamInMatch>().OrderBy(x => x.TeamRole).ToList();
                }

                if (matchToReturn != null)
                {
                    // Add match innings and player innings within that to the match
                    var unprocessedInningsWithBatting = await connection.QueryAsync<MatchInnings, OverSet, MatchTeamIds, PlayerInnings, PlayerDto, PlayerDto, PlayerDto, MatchInnings>(
                        $@"SELECT i.MatchInningsId, i.Byes, i.Wides, i.NoBalls, i.BonusOrPenaltyRuns, i.Runs, i.Wickets, i.InningsOrderInMatch,
                               os.OverSetId, os.OverSetNumber, os.Overs, os.BallsPerOver,
                               i.BattingMatchTeamId, i.BowlingMatchTeamId,
                               pi.PlayerInningsId, pi.BattingPosition, pi.DismissalType, pi.RunsScored, pi.BallsFaced,
                               bat.PlayerIdentityId, bat.PlayerIdentityName, bat2.PlayerId, bat2.PlayerRoute,
                               field.PlayerIdentityId, field.PlayerIdentityName, field2.PlayerId, field2.PlayerRoute,
                               bowl.PlayerIdentityId, bowl.PlayerIdentityName, bowl2.PlayerId, bowl2.PlayerRoute
                               FROM {Tables.MatchInnings} i 
                               LEFT JOIN {Tables.OverSet} os ON i.MatchInningsId = os.MatchInningsId
                               LEFT JOIN {Tables.PlayerInnings} pi ON i.MatchInningsId = pi.MatchInningsId
                               LEFT JOIN {Tables.PlayerIdentity} bat ON pi.BatterPlayerIdentityId = bat.PlayerIdentityId
                               LEFT JOIN {Tables.Player} bat2 ON bat.PlayerId = bat2.PlayerId
                               LEFT JOIN {Tables.PlayerIdentity} field ON pi.DismissedByPlayerIdentityId = field.PlayerIdentityId
                               LEFT JOIN {Tables.Player} field2 ON field.PlayerId = field2.PlayerId
                               LEFT JOIN {Tables.PlayerIdentity} bowl ON pi.BowlerPlayerIdentityId = bowl.PlayerIdentityId
                               LEFT JOIN {Tables.Player} bowl2 ON bowl.PlayerId = bowl2.PlayerId
                               WHERE i.MatchId = @MatchId
                               ORDER BY i.InningsOrderInMatch, pi.BattingPosition",
                        (innings, overSet, matchTeamIds, batting, batter, dismissedBy, bowledBy) =>
                        {
                            if (overSet != null)
                            {
                                innings.OverSets.Add(overSet);
                            }
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
                                batting.Batter = new PlayerIdentity
                                {
                                    Player = new Player
                                    {
                                        PlayerId = batter.PlayerId,
                                        PlayerRoute = batter.PlayerRoute
                                    },
                                    PlayerIdentityId = batter.PlayerIdentityId,
                                    PlayerIdentityName = batter.PlayerIdentityName,
                                    Team = innings.BattingTeam.Team
                                };
                                if (dismissedBy != null)
                                {
                                    batting.DismissedBy = new PlayerIdentity
                                    {
                                        Player = new Player
                                        {
                                            PlayerId = dismissedBy.PlayerId,
                                            PlayerRoute = dismissedBy.PlayerRoute
                                        },
                                        PlayerIdentityId = dismissedBy.PlayerIdentityId,
                                        PlayerIdentityName = dismissedBy.PlayerIdentityName,
                                        Team = innings.BowlingTeam.Team
                                    };
                                }
                                if (bowledBy != null)
                                {
                                    batting.Bowler = new PlayerIdentity
                                    {
                                        Player = new Player
                                        {
                                            PlayerId = bowledBy.PlayerId,
                                            PlayerRoute = bowledBy.PlayerRoute
                                        },
                                        PlayerIdentityId = bowledBy.PlayerIdentityId,
                                        PlayerIdentityName = bowledBy.PlayerIdentityName,
                                        Team = innings.BowlingTeam.Team
                                    };
                                }
                                innings.PlayerInnings.Add(batting);
                            }
                            return innings;
                        },
                        new { matchToReturn.MatchId },
                        splitOn: "OverSetId, BattingMatchTeamId, PlayerInningsId, PlayerIdentityId, PlayerIdentityId, PlayerIdentityId")
                        .ConfigureAwait(false);

                    matchToReturn.MatchInnings = unprocessedInningsWithBatting.GroupBy(x => x.MatchInningsId).Select(inningsRows =>
                    {
                        var innings = inningsRows.First();
                        innings.OverSets = inningsRows
                                .Select(inningsRow => inningsRow.OverSets.SingleOrDefault())
                                .OfType<OverSet>()
                                .Distinct(new OverSetEqualityComparer())
                                .OrderBy(x => x.OverSetNumber)
                                .ToList();
                        innings.PlayerInnings = inningsRows
                                .Select(inningsRow => inningsRow.PlayerInnings.SingleOrDefault())
                                .OfType<PlayerInnings>()
                                .ToList();
                        return innings;

                    }).OrderBy(x => x.InningsOrderInMatch).ToList();

                    // We now have the match innings. Get the overs recorded for them.
                    var unprocessedInningsWithOvers = await connection.QueryAsync<MatchInnings, Over, PlayerIdentity, Player, Team, MatchInnings>(
                             $@"SELECT i.MatchInningsId,
                                    o.OverNumber, o.BallsBowled, o.NoBalls, o.Wides, o.RunsConceded,
                                    pi.PlayerIdentityId, pi.PlayerIdentityName, 
                                    p.PlayerId, p.PlayerRoute,
                                    pi.TeamId
                                    FROM {Tables.MatchInnings} i 
                                    INNER JOIN {Tables.Over} o ON i.MatchInningsId = o.MatchInningsId
                                    INNER JOIN {Tables.PlayerIdentity} pi ON o.BowlerPlayerIdentityId = pi.PlayerIdentityId
                                    INNER JOIN {Tables.Player} p ON pi.PlayerId = p.PlayerId
                                    WHERE i.MatchId = @MatchId
                                    ORDER BY i.InningsOrderInMatch, o.OverNumber",
                             (innings, over, bowlerPlayerIdentity, bowlerPlayer, team) =>
                             {
                                 over.Bowler = bowlerPlayerIdentity;
                                 over.Bowler.Player = bowlerPlayer;
                                 over.Bowler.Team = team;
                                 innings.OversBowled.Add(over);
                                 return innings;
                             },
                             new { matchToReturn.MatchId },
                             splitOn: "OverNumber, PlayerIdentityId, PlayerId, TeamId")
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
                    var bowlingFigures = await connection.QueryAsync<BowlingFigures, MatchInnings, PlayerIdentity, Player, Team, BowlingFigures>
                        ($@"SELECT bf.Overs, bf.Maidens, bf.RunsConceded, bf.Wickets,
                            bf.MatchInningsId,
                            pi.PlayerIdentityId, pi.PlayerIdentityName,
                            p.PlayerId, p.PlayerRoute,
                            pi.TeamId
                            FROM {Tables.BowlingFigures} bf
                            INNER JOIN {Tables.PlayerIdentity} pi ON bf.BowlerPlayerIdentityId = pi.PlayerIdentityId
                            INNER JOIN {Tables.Player} p ON pi.PlayerId = p.PlayerId
                            WHERE bf.MatchInningsId IN @MatchInningsIds
                            ORDER BY bf.MatchInningsId, bf.BowlingOrder",
                            (bowling, innings, bowlerPlayerIdentity, bowlerPlayer, team) =>
                            {
                                bowling.MatchInnings = innings;
                                bowling.Bowler = bowlerPlayerIdentity;
                                bowling.Bowler.Player = bowlerPlayer;
                                bowling.Bowler.Team = team;
                                return bowling;
                            },
                            new { MatchInningsIds = matchToReturn.MatchInnings.Select(x => x.MatchInningsId) },
                            splitOn: "MatchInningsId, PlayerIdentityId, PlayerId, TeamId").ConfigureAwait(false);

                    foreach (var innings in matchToReturn.MatchInnings)
                    {
                        innings.BowlingFigures.AddRange(bowlingFigures.Where(x => x.MatchInnings.MatchInningsId == innings.MatchInningsId));
                    }

                    // Add awards - player of the match etc - to the match
                    matchToReturn.Awards = (await connection.QueryAsync<MatchAward, Award, PlayerIdentity, Player, Team, MatchAward>(
                        $@"SELECT ma.AwardedToId, ma.Reason, a.AwardName, 
                               pi.PlayerIdentityId, pi.PlayerIdentityName, 
                               p.PlayerId, p.PlayerRoute,
                               pi.TeamId
                               FROM {Tables.AwardedTo} ma
                               INNER JOIN {Tables.Award} a ON ma.AwardId = a.AwardId
                               INNER JOIN {Tables.PlayerIdentity} pi ON ma.PlayerIdentityId = pi.PlayerIdentityId
                               INNER JOIN {Tables.Player} p ON pi.PlayerId = p.PlayerId
                               WHERE ma.MatchId = @MatchId
                               ORDER BY a.AwardName",
                        (matchAward, award, playerIdentity, player, team) =>
                        {
                            matchAward.Award = award;
                            matchAward.PlayerIdentity = playerIdentity;
                            matchAward.PlayerIdentity.Player = player;
                            matchAward.PlayerIdentity.Team = team;
                            return matchAward;
                        },
                        new { matchToReturn.MatchId },
                        splitOn: "AwardName, PlayerIdentityId, PlayerId, TeamId").ConfigureAwait(false)).OrderBy(x => x.Award.AwardName).ToList();
                }

                return matchToReturn;
            }
        }
    }
}
