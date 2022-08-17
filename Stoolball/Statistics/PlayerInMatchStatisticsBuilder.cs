using System;
using System.Collections.Generic;
using System.Linq;
using Stoolball.Matches;

namespace Stoolball.Statistics
{
    public class PlayerInMatchStatisticsBuilder : IPlayerInMatchStatisticsBuilder
    {
        private readonly IPlayerIdentityFinder _playerIdentityFinder;
        private readonly IOversHelper _oversHelper;

        public PlayerInMatchStatisticsBuilder(IPlayerIdentityFinder playerIdentityFinder, IOversHelper oversHelper)
        {
            _playerIdentityFinder = playerIdentityFinder ?? throw new ArgumentNullException(nameof(playerIdentityFinder));
            _oversHelper = oversHelper;
        }

        public IEnumerable<PlayerInMatchStatisticsRecord> BuildStatisticsForMatch(Match match)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            if (match.MatchInnings.Count == 0)
            {
                throw new ArgumentException($"{nameof(match)} {match.MatchId} must have innings data");
            }

            if (match.Teams.Count != 2)
            {
                throw new ArgumentException($"{nameof(match)} {match.MatchId} must have a home and away team");
            }

            var homeTeam = match.Teams.Single(t => t.TeamRole == TeamRole.Home);
            var awayTeam = match.Teams.Single(t => t.TeamRole == TeamRole.Away);
            var homePlayers = _playerIdentityFinder.PlayerIdentitiesInMatch(match, TeamRole.Home);
            var awayPlayers = _playerIdentityFinder.PlayerIdentitiesInMatch(match, TeamRole.Away);

            if (homePlayers.Any(x => !x.PlayerIdentityId.HasValue) || awayPlayers.Any(x => !x.PlayerIdentityId.HasValue))
            {
                throw new ArgumentException($"All player identities in match {match.MatchId} must have a PlayerIdentityId");
            }

            if (homePlayers.Any(x => x.Player?.PlayerId == null) || awayPlayers.Any(x => x.Player?.PlayerId == null))
            {
                throw new ArgumentException($"All player identities in match {match.MatchId} must have a PlayerId. Player identity ");
            }

            var records = new List<PlayerInMatchStatisticsRecord>();

            foreach (var innings in match.MatchInnings)
            {
                if (!innings.BattingMatchTeamId.HasValue && innings.BattingTeam?.MatchTeamId != null)
                {
                    innings.BattingMatchTeamId = innings.BattingTeam.MatchTeamId;
                }

                if (!innings.BattingMatchTeamId.HasValue)
                {
                    throw new ArgumentException($"{nameof(match)} must have the BattingMatchTeamId for each MatchInnings");
                }

                var homeTeamIsBatting = innings.BattingMatchTeamId == homeTeam.MatchTeamId;
                var batters = homeTeamIsBatting ? homePlayers : awayPlayers;
                var fielders = homeTeamIsBatting ? awayPlayers : homePlayers;

                foreach (var batter in batters)
                {
                    FindOrCreateInningsRecordsForBatter(records, match, homeTeam, awayTeam, innings, homeTeamIsBatting, batter);
                }

                foreach (var fielder in fielders)
                {
                    FindOrCreateInningsRecordForFielder(records, match, homeTeam, awayTeam, innings, homeTeamIsBatting, fielder);
                }
            }

            foreach (var playerRecord in records.Where(x => x.MatchTeamId == homeTeam.MatchTeamId))
            {
                var identity = homePlayers.Single(x => x.PlayerIdentityId == playerRecord.PlayerIdentityId);

                if (match.InningsOrderIsKnown)
                {
                    playerRecord.BattedFirst = match.MatchInnings[0].BattingMatchTeamId == homeTeam.MatchTeamId;
                }
                playerRecord.WonToss = homeTeam.WonToss;
                playerRecord.WonMatch = DidThePlayerWinTheMatch(homeTeam.MatchTeamId.Value, match);
                playerRecord.PlayerOfTheMatch = match.Awards.Any(x => x.Award.AwardName.ToUpperInvariant() == StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD && x.PlayerIdentity.PlayerIdentityId == identity.PlayerIdentityId);
            }

            foreach (var playerRecord in records.Where(x => x.MatchTeamId == awayTeam.MatchTeamId))
            {
                var identity = awayPlayers.Single(x => x.PlayerIdentityId == playerRecord.PlayerIdentityId);

                if (match.InningsOrderIsKnown)
                {
                    playerRecord.BattedFirst = match.MatchInnings[0].BattingMatchTeamId == awayTeam.MatchTeamId;
                }
                playerRecord.WonToss = awayTeam.WonToss;
                playerRecord.WonMatch = DidThePlayerWinTheMatch(awayTeam.MatchTeamId.Value, match);
                playerRecord.PlayerOfTheMatch = match.Awards.Any(x => x.Award.AwardName.ToUpperInvariant() == StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD && x.PlayerIdentity.PlayerIdentityId == identity.PlayerIdentityId);
            }

            return records;
        }

        private void FindOrCreateInningsRecordForFielder(List<PlayerInMatchStatisticsRecord> records, Match match, TeamInMatch homeTeam, TeamInMatch awayTeam, MatchInnings innings, bool homeTeamIsBatting, PlayerIdentity fielder)
        {
            var record = records.SingleOrDefault(x => x.MatchTeamId == innings.BowlingMatchTeamId && x.PlayerIdentityId == fielder.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
            if (record == null)
            {
                record = CreateRecordForPlayerInInningsPair(match, innings, fielder, homeTeamIsBatting ? awayTeam : homeTeam, homeTeamIsBatting ? homeTeam : awayTeam);
                record.PlayerInningsNumber = null;
                records.Add(record);
            }
            var bowlingFigures = innings.BowlingFigures.SingleOrDefault(x => x.Bowler.PlayerIdentityId == fielder.PlayerIdentityId);
            var oversBowled = innings.OversBowled.Where(x => x.Bowler.PlayerIdentityId == fielder.PlayerIdentityId);

            record.Catches = innings.PlayerInnings.Count(x => (x.DismissalType == DismissalType.Caught && x.DismissedBy?.PlayerIdentityId == fielder.PlayerIdentityId) ||
                                                          (x.DismissalType == DismissalType.CaughtAndBowled && x.Bowler?.PlayerIdentityId == fielder.PlayerIdentityId));
            record.RunOuts = innings.PlayerInnings.Count(x => x.DismissalType == DismissalType.RunOut && x.DismissedBy?.PlayerIdentityId == fielder.PlayerIdentityId);

            record.BowlingFiguresId = bowlingFigures?.BowlingFiguresId;
            record.OverNumberOfFirstOverBowled = innings.OversBowled.OrderBy(x => x.OverNumber).FirstOrDefault(x => x.Bowler.PlayerIdentityId == fielder.PlayerIdentityId)?.OverNumber;
            if (oversBowled.Any())
            {
                record.BallsBowled = oversBowled.Where(x => x.BallsBowled.HasValue).Sum(x => x.BallsBowled) + (oversBowled.Count(x => !x.BallsBowled.HasValue) * StatisticsConstants.BALLS_PER_OVER);
                record.NoBalls = oversBowled.Sum(x => x.NoBalls);
                record.Wides = oversBowled.Sum(x => x.Wides);
            }
            else
            {
                record.BallsBowled = bowlingFigures?.Overs != null ? _oversHelper.OversToBallsBowled(bowlingFigures.Overs.Value) : (int?)null;
            }
            record.Overs = bowlingFigures?.Overs;
            record.Maidens = bowlingFigures?.Maidens;
            record.RunsConceded = bowlingFigures?.RunsConceded;
            record.HasRunsConceded = bowlingFigures?.RunsConceded != null;
            record.Wickets = bowlingFigures?.Wickets;
            record.WicketsWithBowling = (bowlingFigures != null && oversBowled.Any()) ? bowlingFigures.Wickets : (int?)null;
        }

        private static void FindOrCreateInningsRecordsForBatter(List<PlayerInMatchStatisticsRecord> records, Match match, TeamInMatch homeTeam, TeamInMatch awayTeam, MatchInnings innings, bool homeTeamIsBatting, PlayerIdentity batter)
        {
            var allPlayerInningsForThisPlayer = innings.PlayerInnings.Where(x => x.Batter.PlayerIdentityId == batter.PlayerIdentityId).OrderBy(x => x.BattingPosition).ToList();
            var firstPlayerInningsForThisPlayer = allPlayerInningsForThisPlayer.FirstOrDefault();

            // Find or add a record every batting team member in this innings regardless of whether they are recorded as batting
            var record = records.SingleOrDefault(x => x.MatchTeamId == innings.BattingMatchTeamId && x.PlayerIdentityId == batter.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
            if (record == null)
            {
                record = CreateRecordForPlayerInInningsPair(match, innings, batter, homeTeamIsBatting ? homeTeam : awayTeam, homeTeamIsBatting ? awayTeam : homeTeam);
                records.Add(record);
            }
            record.PlayerInningsNumber = 1;
            AddPlayerInningsDataToRecord(firstPlayerInningsForThisPlayer, record);
            // There may be player innings with DismissalType = null, but that means they *were* dismissed, so for players who are missing from the batting card assume DidNotBat rather than null
            record.DismissalType = firstPlayerInningsForThisPlayer != null ? firstPlayerInningsForThisPlayer.DismissalType : DismissalType.DidNotBat;
            record.PlayerWasDismissed = firstPlayerInningsForThisPlayer != null ? StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(firstPlayerInningsForThisPlayer.DismissalType) : false;

            // Add extra records for any players who batted multiple times in the same innings
            for (var i = 1; i < allPlayerInningsForThisPlayer.Count; i++)
            {
                var extraBattingRecord = records.SingleOrDefault(x => x.MatchTeamId == innings.BattingMatchTeamId && x.PlayerIdentityId == batter.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && x.PlayerInningsNumber == i + 1);
                if (extraBattingRecord == null)
                {
                    extraBattingRecord = CreateRecordForPlayerInInningsPair(match, innings, batter, homeTeamIsBatting ? homeTeam : awayTeam, homeTeamIsBatting ? awayTeam : homeTeam);
                    extraBattingRecord.PlayerInningsNumber = i + 1;
                    AddPlayerInningsDataToRecord(allPlayerInningsForThisPlayer[i], extraBattingRecord);
                    extraBattingRecord.DismissalType = allPlayerInningsForThisPlayer[i].DismissalType;
                    extraBattingRecord.PlayerWasDismissed = StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(allPlayerInningsForThisPlayer[i].DismissalType);
                    records.Add(extraBattingRecord);
                }
            }
        }

        private static int? DidThePlayerWinTheMatch(Guid matchTeamId, Match match)
        {
            var isHomePlayer = matchTeamId == match.Teams.Single(x => x.TeamRole == TeamRole.Home).MatchTeamId;
            if (match.MatchResultType == MatchResultType.HomeWin)
            {
                return isHomePlayer ? 1 : -1;
            }
            else if (match.MatchResultType == MatchResultType.Tie)
            {
                return 0;
            }
            else if (match.MatchResultType == MatchResultType.AwayWin)
            {
                return isHomePlayer ? -1 : 1;
            }
            else
            {
                return null;
            }
        }

        private static void AddPlayerInningsDataToRecord(PlayerInnings playerInnings, PlayerInMatchStatisticsRecord record)
        {
            record.PlayerInningsId = playerInnings?.PlayerInningsId;
            record.BattingPosition = playerInnings?.BattingPosition == 2 ? 1 : playerInnings?.BattingPosition;
            record.RunsScored = playerInnings?.RunsScored;
            record.BallsFaced = playerInnings?.BallsFaced;

            if (playerInnings != null && StatisticsConstants.DISMISSALS_CREDITED_TO_BOWLER.Contains(playerInnings.DismissalType))
            {
                record.BowledByPlayerIdentityId = playerInnings.Bowler?.PlayerIdentityId;
            }

            if (playerInnings != null && playerInnings.DismissalType == DismissalType.Caught)
            {
                record.CaughtByPlayerIdentityId = playerInnings.DismissedBy?.PlayerIdentityId;
            }

            if (playerInnings != null && playerInnings.DismissalType == DismissalType.CaughtAndBowled)
            {
                record.CaughtByPlayerIdentityId = playerInnings.Bowler?.PlayerIdentityId;
            }

            if (playerInnings != null && playerInnings.DismissalType == DismissalType.RunOut)
            {
                record.RunOutByPlayerIdentityId = playerInnings.DismissedBy?.PlayerIdentityId;
            }
        }

        private static PlayerInMatchStatisticsRecord CreateRecordForPlayerInInningsPair(Match match, MatchInnings innings, PlayerIdentity identity, TeamInMatch team, TeamInMatch opposition)
        {
            var isOnBattingTeam = team.MatchTeamId == innings.BattingMatchTeamId;
            var pairedInnings = match.MatchInnings.Single(x => x.InningsPair() == innings.InningsPair() && x.MatchInningsId != innings.MatchInningsId);

            return new PlayerInMatchStatisticsRecord
            {
                PlayerId = identity.Player.PlayerId.Value,
                PlayerIdentityId = identity.PlayerIdentityId.Value,
                MatchId = match.MatchId.Value,
                MatchInningsPair = innings.InningsPair(),
                TeamRunsScored = isOnBattingTeam ? innings.Runs : pairedInnings.Runs,
                TeamWicketsLost = isOnBattingTeam ? innings.Wickets : pairedInnings.Wickets,
                TeamBonusOrPenaltyRunsAwarded = isOnBattingTeam ? innings.BonusOrPenaltyRuns : pairedInnings.BonusOrPenaltyRuns,
                TeamRunsConceded = isOnBattingTeam ? pairedInnings.Runs : innings.Runs,
                TeamNoBallsConceded = isOnBattingTeam ? pairedInnings.NoBalls : innings.NoBalls,
                TeamWidesConceded = isOnBattingTeam ? pairedInnings.Wides : innings.Wides,
                TeamByesConceded = isOnBattingTeam ? pairedInnings.Byes : innings.Byes,
                TeamWicketsTaken = isOnBattingTeam ? pairedInnings.Wickets : innings.Wickets,
                MatchTeamId = team.MatchTeamId.Value,
                OppositionTeamId = opposition.Team.TeamId.Value
            };
        }
    }
}
