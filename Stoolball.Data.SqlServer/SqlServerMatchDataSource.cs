﻿using System;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Awards;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
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
            public string PlayerIdentityName { get; set; } = string.Empty;
            public string PlayerRoute { get; set; } = string.Empty;
        }

        private class TeamDto
        {
            public Guid TeamId { get; set; }
            public string TeamRoute { get; set; } = string.Empty;
            public string TeamName { get; set; } = string.Empty;
            public string MemberGroupName { get; set; } = string.Empty;
            public Guid? ClubId { get; set; }
        }

        /// <summary>
        /// Gets a single stoolball match based on its route
        /// </summary>
        /// <param name="route">/matches/example-match</param>
        /// <returns>A matching <see cref="Match"/> or <c>null</c> if not found</returns>
        public async Task<Match?> ReadMatchByRoute(string route)
        {
            var normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(route, "matches");

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var matches = await connection.QueryAsync<Match, Tournament, TeamInMatch, TeamDto, MatchLocation, Season, Competition, Match>(
                    $@"SELECT m.MatchId, m.MatchName, m.MatchType, m.PlayerType, m.StartTime, m.StartTimeIsKnown, m.MatchResultType, m.PlayersPerTeam,
                            m.LastPlayerBatsOn, m.EnableBonusOrPenaltyRuns, m.InningsOrderIsKnown, m.MatchNotes, m.MatchRoute, m.MemberKey, m.UpdateMatchNameAutomatically,
                            tourney.TournamentId, tourney.TournamentRoute, tourney.TournamentName, tourney.MemberKey,
                            mt.MatchTeamId, mt.TeamRole, mt.WonToss,
                            t.TeamId, t.TeamRoute, tn.TeamName, t.MemberGroupName, t.ClubId,
                            ml.MatchLocationId, ml.MatchLocationRoute, ml.SecondaryAddressableObjectName, ml.PrimaryAddressableObjectName, ml.StreetDescription,
                            ml.Locality, ml.Town, ml.AdministrativeArea, ml.Postcode, ml.Latitude, ml.Longitude, ml.GeoPrecision, ml.MatchLocationNotes,
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
                            teamInMatch.Team = new Team { TeamId = team.TeamId, TeamName = team.TeamName, TeamRoute = team.TeamRoute, MemberGroupName = team.MemberGroupName };
                            if (team.ClubId.HasValue)
                            {
                                teamInMatch.Team.Club = new Club { ClubId = team.ClubId };
                            }
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
                    if (matchToReturn.MatchType == MatchType.TrainingSession) { return matchToReturn; }

                    // Add match innings and player innings within that to the match
                    var unprocessedInningsWithBatting = await connection.QueryAsync<MatchInnings, MatchTeamIds, PlayerInnings, PlayerDto, PlayerDto, PlayerDto, MatchInnings>(
                        $@"SELECT i.MatchInningsId, i.Byes, i.Wides, i.NoBalls, i.BonusOrPenaltyRuns, i.Runs, i.Wickets, i.InningsOrderInMatch,
                               i.BattingMatchTeamId, i.BowlingMatchTeamId,
                               pi.PlayerInningsId, pi.BattingPosition, pi.DismissalType, pi.RunsScored, pi.BallsFaced,
                               bat.PlayerIdentityId, bat.PlayerIdentityName, bat.PlayerId, bat.PlayerRoute,
                               field.PlayerIdentityId, field.PlayerIdentityName, field.PlayerId, field.PlayerRoute,
                               bowl.PlayerIdentityId, bowl.PlayerIdentityName, bowl.PlayerId, bowl.PlayerRoute
                               FROM {Tables.MatchInnings} i 
                               LEFT JOIN {Tables.PlayerInnings} pi ON i.MatchInningsId = pi.MatchInningsId
                               LEFT JOIN {Views.PlayerIdentity} bat ON pi.BatterPlayerIdentityId = bat.PlayerIdentityId
                               LEFT JOIN {Views.PlayerIdentity} field ON pi.DismissedByPlayerIdentityId = field.PlayerIdentityId
                               LEFT JOIN {Views.PlayerIdentity} bowl ON pi.BowlerPlayerIdentityId = bowl.PlayerIdentityId
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
                                innings.BowlingMatchTeamId = matchTeamIds.BowlingMatchTeamId;
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
                        splitOn: "BattingMatchTeamId, PlayerInningsId, PlayerIdentityId, PlayerIdentityId, PlayerIdentityId")
                        .ConfigureAwait(false);

                    matchToReturn.MatchInnings = unprocessedInningsWithBatting.GroupBy(x => x.MatchInningsId).Select(inningsRows =>
                    {
                        var innings = inningsRows.First();
                        innings.PlayerInnings = inningsRows
                                .Select(inningsRow => inningsRow.PlayerInnings.SingleOrDefault())
                                .OfType<PlayerInnings>()
                                .ToList();
                        return innings;

                    }).OrderBy(x => x.InningsOrderInMatch).ToList();

                    // We now have the match innings. Get the overs recorded for them.
                    var unprocessedInningsWithOvers = await connection.QueryAsync<MatchInnings, OverSet, Over, PlayerIdentity, Player, Team, MatchInnings>(
                             $@"SELECT i.MatchInningsId,
		                            os.OverSetId, os.OverSetNumber, os.Overs, os.BallsPerOver,
                                    o.OverNumber, o.BallsBowled, o.NoBalls, o.Wides, o.RunsConceded,
                                    pi.PlayerIdentityId, pi.PlayerIdentityName, 
                                    pi.PlayerId, pi.PlayerRoute,
                                    pi.TeamId
                                    FROM {Tables.MatchInnings} i 
                                    INNER JOIN {Tables.OverSet} os ON i.MatchInningsId = os.MatchInningsId
                                    LEFT JOIN {Tables.Over} o ON os.OverSetId = o.OverSetId
                                    LEFT JOIN {Views.PlayerIdentity} pi ON o.BowlerPlayerIdentityId = pi.PlayerIdentityId
                                    WHERE i.MatchId = @MatchId
                                    ORDER BY i.InningsOrderInMatch, o.OverNumber",
                             (innings, overSet, over, bowlerPlayerIdentity, bowlerPlayer, team) =>
                             {
                                 if (overSet != null)
                                 {
                                     innings.OverSets.Add(overSet);
                                 }

                                 if (over != null)
                                 {
                                     over.Bowler = bowlerPlayerIdentity;
                                     over.Bowler.Player = bowlerPlayer;
                                     over.Bowler.Team = team;
                                     innings.OversBowled.Add(over);
                                 }
                                 return innings;
                             },
                             new { matchToReturn.MatchId },
                             splitOn: "OverSetId, OverNumber, PlayerIdentityId, PlayerId, TeamId")
                             .ConfigureAwait(false);

                    // Add those overs to the existing instances of the match innings.
                    var processedInningsWithOvers = unprocessedInningsWithOvers.GroupBy(x => x.MatchInningsId).Select(inningsRows =>
                    {
                        var innings = inningsRows.First();
                        innings.OverSets = inningsRows
                            .Select(inningsRow => inningsRow.OverSets.SingleOrDefault())
                            .OfType<OverSet>()
                            .Distinct(new OverSetEqualityComparer())
                            .OrderBy(x => x.OverSetNumber)
                            .ToList();
                        innings.OversBowled = inningsRows.Select(inningsRow => inningsRow.OversBowled.SingleOrDefault()).OfType<Over>().ToList();
                        return innings;
                    }).ToList();


                    foreach (var innings in matchToReturn.MatchInnings)
                    {
                        var processedInningsData = processedInningsWithOvers.SingleOrDefault(x => x.MatchInningsId == innings.MatchInningsId);
                        if (processedInningsData != null)
                        {
                            innings.OverSets = processedInningsData.OverSets;
                            innings.OversBowled = processedInningsData.OversBowled;
                        }
                    }

                    // Add bowling figures
                    var bowlingFigures = await connection.QueryAsync<BowlingFigures, MatchInnings, PlayerIdentity, Player, Team, BowlingFigures>
                        ($@"SELECT bf.BowlingFiguresId, bf.Overs, bf.Maidens, bf.RunsConceded, bf.Wickets,
                            bf.MatchInningsId,
                            pi.PlayerIdentityId, pi.PlayerIdentityName,
                            pi.PlayerId, pi.PlayerRoute,
                            pi.TeamId
                            FROM {Tables.BowlingFigures} bf
                            INNER JOIN {Views.PlayerIdentity} pi ON bf.BowlerPlayerIdentityId = pi.PlayerIdentityId
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
                               pi.PlayerId, pi.PlayerRoute,
                               pi.TeamId
                               FROM {Tables.AwardedTo} ma
                               INNER JOIN {Tables.Award} a ON ma.AwardId = a.AwardId
                               INNER JOIN {Views.PlayerIdentity} pi ON ma.PlayerIdentityId = pi.PlayerIdentityId
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
