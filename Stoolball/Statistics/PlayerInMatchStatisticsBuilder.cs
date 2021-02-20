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

            var allPlayers = _playerIdentityFinder.PlayerIdentitiesInMatch(match);

            if (allPlayers.Any(x => !x.PlayerIdentityId.HasValue))
            {
                throw new ArgumentException($"All player identities in match {match.MatchId} must have a PlayerIdentityId");
            }

            if (allPlayers.Any(x => x.Player?.PlayerId == null))
            {
                throw new ArgumentException($"All player identities in match {match.MatchId} must have a PlayerId");
            }

            var homeTeam = match.Teams.Single(t => t.TeamRole == TeamRole.Home);
            var awayTeam = match.Teams.Single(t => t.TeamRole == TeamRole.Away);
            var homePlayers = allPlayers.Where(x => x.Team.TeamId == homeTeam.Team.TeamId);
            var awayPlayers = allPlayers.Where(x => x.Team.TeamId == awayTeam.Team.TeamId);

            var records = new List<PlayerInMatchStatisticsRecord>();

            foreach (var innings in match.MatchInnings)
            {
                if (innings.BattingTeam == null || innings.BowlingTeam == null)
                {
                    throw new ArgumentException($"{nameof(match)} must have the BattingTeam and BowlingTeam set for each MatchInnings");
                }

                var homeTeamIsBatting = innings.BattingTeam.Team.TeamId == homeTeam.Team.TeamId;
                var batters = homeTeamIsBatting ? homePlayers : awayPlayers;
                var fielders = homeTeamIsBatting ? awayPlayers : homePlayers;

                foreach (var batter in batters)
                {
                    records.AddRange(CreateInningsRecordsForBatter(match, homeTeam, awayTeam, innings, homeTeamIsBatting, batter));
                }

                foreach (var fielder in fielders)
                {
                    // Add a record for every fielding team member in this innings
                    records.Add(CreateInningsRecordForFielder(match, homeTeam, awayTeam, innings, homeTeamIsBatting, fielder));
                }
            }

            return records;
        }

        private PlayerInMatchStatisticsRecord CreateInningsRecordForFielder(Match match, TeamInMatch homeTeam, TeamInMatch awayTeam, MatchInnings innings, bool homeTeamIsBatting, PlayerIdentity fielder)
        {
            var record = CreateRecordForPlayerInInnings(match, innings, fielder, homeTeamIsBatting ? awayTeam : homeTeam, homeTeamIsBatting ? homeTeam : awayTeam);
            var bowlingFigures = innings.BowlingFigures.SingleOrDefault(x => x.Bowler.PlayerIdentityId == fielder.PlayerIdentityId);
            var oversBowled = innings.OversBowled.Where(x => x.Bowler.PlayerIdentityId == fielder.PlayerIdentityId);

            record.PlayerInningsInMatchInnings = null;
            record.Catches = innings.PlayerInnings.Count(x => (x.DismissalType == DismissalType.Caught && x.DismissedBy?.PlayerIdentityId == fielder.PlayerIdentityId) ||
                                                          (x.DismissalType == DismissalType.CaughtAndBowled && x.Bowler?.PlayerIdentityId == fielder.PlayerIdentityId));
            record.RunOuts = innings.PlayerInnings.Count(x => x.DismissalType == DismissalType.RunOut && x.DismissedBy?.PlayerIdentityId == fielder.PlayerIdentityId);

            record.OverNumberOfFirstOverBowled = innings.OversBowled.OrderBy(x => x.OverNumber).FirstOrDefault(x => x.Bowler.PlayerIdentityId == fielder.PlayerIdentityId)?.OverNumber;
            if (oversBowled.Any())
            {
                record.BallsBowled = oversBowled.Sum(x => x.BallsBowled);
            }
            else
            {
                record.BallsBowled = bowlingFigures?.Overs != null ? _oversHelper.OversToBallsBowled(bowlingFigures.Overs.Value) : (int?)null;
            }
            record.Overs = bowlingFigures?.Overs;
            record.Maidens = bowlingFigures?.Maidens;
            record.RunsConceded = bowlingFigures?.RunsConceded;
            record.HasRunsConceded = bowlingFigures != null ? bowlingFigures.RunsConceded != null : (bool?)null;
            record.Wickets = bowlingFigures?.Wickets;
            record.WicketsWithBowling = (bowlingFigures != null && oversBowled.Any()) ? bowlingFigures.Wickets : (int?)null;

            record.WonMatch = DidThePlayerWinTheMatch(fielder, match);
            record.PlayerOfTheMatch = match.Awards.Any(x => x.Award.AwardName.ToUpperInvariant() == StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD && x.PlayerIdentity.PlayerIdentityId == fielder.PlayerIdentityId);
            return record;
        }

        private static List<PlayerInMatchStatisticsRecord> CreateInningsRecordsForBatter(Match match, TeamInMatch homeTeam, TeamInMatch awayTeam, MatchInnings innings, bool homeTeamIsBatting, PlayerIdentity batter)
        {
            var records = new List<PlayerInMatchStatisticsRecord>();

            var allPlayerInningsForThisPlayer = innings.PlayerInnings.Where(x => x.Batter.PlayerIdentityId == batter.PlayerIdentityId).OrderBy(x => x.BattingPosition).ToList();
            var firstPlayerInningsForThisPlayer = allPlayerInningsForThisPlayer.FirstOrDefault();

            // Add a record every batting team member in this innings regardless of whether they are recorded as batting
            var record = CreateRecordForPlayerInInnings(match, innings, batter, homeTeamIsBatting ? homeTeam : awayTeam, homeTeamIsBatting ? awayTeam : homeTeam);
            record.PlayerInningsInMatchInnings = 1;
            AddPlayerInningsDataToRecord(firstPlayerInningsForThisPlayer, record);
            // There may be player innings with DismissalType = null, but that means they *were* dismissed, so for players who are missing from the batting card assume DidNotBat rather than null
            record.DismissalType = firstPlayerInningsForThisPlayer != null ? firstPlayerInningsForThisPlayer.DismissalType : DismissalType.DidNotBat;
            record.PlayerWasDismissed = firstPlayerInningsForThisPlayer != null ? StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(firstPlayerInningsForThisPlayer.DismissalType) : false;
            records.Add(record);

            // Add extra records for any players who batted multiple times in the same innings
            for (var i = 1; i < allPlayerInningsForThisPlayer.Count; i++)
            {
                records.Add(CreateRecordForPlayerInInnings(match, innings, batter, homeTeamIsBatting ? homeTeam : awayTeam, homeTeamIsBatting ? awayTeam : homeTeam));
                records[records.Count - 1].PlayerInningsInMatchInnings = i + 1;
                AddPlayerInningsDataToRecord(allPlayerInningsForThisPlayer[i], records[records.Count - 1]);
                records[records.Count - 1].DismissalType = allPlayerInningsForThisPlayer[i].DismissalType;
                records[records.Count - 1].PlayerWasDismissed = StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(allPlayerInningsForThisPlayer[i].DismissalType);
            }

            foreach (var playerRecord in records)
            {
                playerRecord.WonMatch = DidThePlayerWinTheMatch(batter, match);
                playerRecord.PlayerOfTheMatch = match.Awards.Any(x => x.Award.AwardName.ToUpperInvariant() == StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD && x.PlayerIdentity.PlayerIdentityId == batter.PlayerIdentityId);
            }

            return records;
        }

        private static int? DidThePlayerWinTheMatch(PlayerIdentity identity, Match match)
        {
            var isHomePlayer = identity.Team.TeamId == match.Teams.Single(x => x.TeamRole == TeamRole.Home).Team.TeamId;
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
            record.BattingPosition = playerInnings?.BattingPosition;
            record.RunsScored = playerInnings?.RunsScored;
            record.BallsFaced = playerInnings?.BallsFaced;

            if (playerInnings != null && StatisticsConstants.DISMISSALS_CREDITED_TO_BOWLER.Contains(playerInnings.DismissalType))
            {
                record.BowledByPlayerIdentityId = playerInnings.Bowler?.PlayerIdentityId;
                record.BowledByPlayerIdentityName = playerInnings.Bowler?.PlayerIdentityName;
                record.BowledByPlayerRoute = playerInnings.Bowler?.Player?.PlayerRoute;
            }

            if (playerInnings != null && playerInnings.DismissalType == DismissalType.Caught)
            {
                record.CaughtByPlayerIdentityId = playerInnings.DismissedBy?.PlayerIdentityId;
                record.CaughtByPlayerIdentityName = playerInnings.DismissedBy?.PlayerIdentityName;
                record.CaughtByPlayerRoute = playerInnings.DismissedBy?.Player?.PlayerRoute;
            }

            if (playerInnings != null && playerInnings.DismissalType == DismissalType.CaughtAndBowled)
            {
                record.CaughtByPlayerIdentityId = playerInnings.Bowler?.PlayerIdentityId;
                record.CaughtByPlayerIdentityName = playerInnings.Bowler?.PlayerIdentityName;
                record.CaughtByPlayerRoute = playerInnings.Bowler?.Player?.PlayerRoute;
            }

            if (playerInnings != null && playerInnings.DismissalType == DismissalType.RunOut)
            {
                record.RunOutByPlayerIdentityId = playerInnings.DismissedBy?.PlayerIdentityId;
                record.RunOutByPlayerIdentityName = playerInnings.DismissedBy?.PlayerIdentityName;
                record.RunOutByPlayerRoute = playerInnings.DismissedBy?.Player?.PlayerRoute;
            }
        }

        private static PlayerInMatchStatisticsRecord CreateRecordForPlayerInInnings(Match match, MatchInnings innings, PlayerIdentity identity, TeamInMatch team, TeamInMatch opposition)
        {
            return new PlayerInMatchStatisticsRecord
            {
                PlayerId = identity.Player.PlayerId.Value,
                PlayerIdentityId = identity.PlayerIdentityId.Value,
                PlayerIdentityName = identity.PlayerIdentityName,
                PlayerRoute = identity.Player.PlayerRoute,
                MatchId = match.MatchId.Value,
                MatchName = match.MatchName,
                MatchType = match.MatchType,
                MatchPlayerType = match.PlayerType,
                MatchRoute = match.MatchRoute,
                MatchStartTime = match.StartTime,
                TournamentId = match.Tournament?.TournamentId,
                MatchLocationId = match.MatchLocation?.MatchLocationId,
                SeasonId = match.Season?.SeasonId,
                CompetitionId = match.Season?.Competition?.CompetitionId,
                MatchInningsId = innings.MatchInningsId.Value,
                InningsOrderInMatch = innings.InningsOrderInMatch,
                InningsOrderIsKnown = match.InningsOrderIsKnown,
                MatchInningsRuns = innings.Runs,
                MatchInningsWickets = innings.Wickets,
                MatchTeamId = team.MatchTeamId.Value,
                TeamId = team.Team.TeamId.Value,
                TeamName = team.Team.TeamName,
                TeamRoute = team.Team.TeamRoute,
                OppositionTeamId = opposition.Team.TeamId.Value,
                OppositionTeamName = opposition.Team.TeamName,
                OppositionTeamRoute = opposition.Team.TeamRoute,
            };
        }
    }
}
