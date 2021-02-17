using System;
using System.Collections.Generic;
using System.Linq;
using Stoolball.Matches;
using Stoolball.Teams;

namespace Stoolball.Statistics
{
    public class PlayerInMatchStatisticsBuilder : IPlayerInMatchStatisticsBuilder
    {
        private readonly IPlayerIdentityFinder _playerIdentityFinder;

        public PlayerInMatchStatisticsBuilder(IPlayerIdentityFinder playerIdentityFinder)
        {
            _playerIdentityFinder = playerIdentityFinder ?? throw new ArgumentNullException(nameof(playerIdentityFinder));
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
                    // Add a record every batting team member in this innings regardless of whether they are recorded as batting
                    var record = CreateRecordForPlayerInInnings(match, innings, batter, homeTeamIsBatting ? homeTeam : awayTeam, homeTeamIsBatting ? awayTeam : homeTeam);
                    record.PlayerInningsInMatchInnings = 1;
                    records.Add(record);

                    // Add extra records for any players who batted multiple times in the same innings
                    var playerInningsForThisPlayer = innings.PlayerInnings.Where(x => x.PlayerIdentity.PlayerIdentityId == batter.PlayerIdentityId).OrderBy(x => x.BattingPosition);
                    for (var i = 1; i < playerInningsForThisPlayer.Count(); i++)
                    {
                        records.Add(CreateRecordForPlayerInInnings(match, innings, batter, homeTeamIsBatting ? homeTeam : awayTeam, homeTeamIsBatting ? awayTeam : homeTeam));
                        records[records.Count - 1].PlayerInningsInMatchInnings = i + 1;
                    }
                }

                foreach (var fielder in fielders)
                {
                    // Add a record for every fielding team member in this innings
                    var record = CreateRecordForPlayerInInnings(match, innings, fielder, homeTeamIsBatting ? awayTeam : homeTeam, homeTeamIsBatting ? homeTeam : awayTeam);
                    record.PlayerInningsInMatchInnings = null;
                    records.Add(record);
                }
            }

            return records;
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
