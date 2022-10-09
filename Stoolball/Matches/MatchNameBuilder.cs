using System.Linq;
using Humanizer;

namespace Stoolball.Matches
{
    public class MatchNameBuilder : IMatchNameBuilder
    {
        /// <summary>
        /// Generates the title of a match from match information, eg "Team 1 v Team 2"
        /// </summary>
        public string BuildMatchName(Match match)
        {
            if (match is null)
            {
                throw new System.ArgumentNullException(nameof(match));
            }

            const string tbc = "To be confirmed";

            var homeTeam = match.Teams.SingleOrDefault(x => x.TeamRole == TeamRole.Home)?.Team;
            var homeTeamName = homeTeam?.TeamName ?? tbc;

            var awayTeam = match.Teams.SingleOrDefault(x => x.TeamRole == TeamRole.Away)?.Team;
            var awayTeamName = awayTeam?.TeamName ?? tbc;

            if (match.MatchType == MatchType.TrainingSession)
            {
                var matchName = "Training session" + (match.Teams.Any() ? " for " + match.Teams.Select(x => x.Team?.TeamName).ToList().Humanize() : string.Empty);
                if (match.MatchResultType == MatchResultType.Cancelled)
                {
                    matchName += " (cancelled)";
                }
                return matchName;
            }

            if (!match.MatchResultType.HasValue)
            {
                return homeTeamName + " v " + awayTeamName;
            }
            else if (match.MatchResultType == MatchResultType.HomeWin)
            {
                return homeTeamName + " beat " + awayTeamName;
            }
            else if (match.MatchResultType == MatchResultType.AwayWin)
            {
                return homeTeamName + " lost to " + awayTeamName;
            }
            else if (match.MatchResultType == MatchResultType.HomeWinByForfeit)
            {
                return homeTeamName + " won by forfeit against " + awayTeamName;
            }
            else if (match.MatchResultType == MatchResultType.AwayWinByForfeit)
            {
                return homeTeamName + " forfeit to " + awayTeamName;
            }
            else if (match.MatchResultType == MatchResultType.Tie)
            {
                return homeTeamName + " tied with " + awayTeamName;
            }
            else if (match.MatchResultType == MatchResultType.AbandonedDuringPlayAndCancelled || match.MatchResultType == MatchResultType.AbandonedDuringPlayAndPostponed)
            {
                return homeTeamName + " v " + awayTeamName + " (abandoned)";
            }
            else if (match.MatchResultType == MatchResultType.Cancelled)
            {
                return homeTeamName + " v " + awayTeamName + " (cancelled)";
            }
            else if (match.MatchResultType == MatchResultType.Postponed)
            {
                return homeTeamName + " v " + awayTeamName + " (postponed)";
            }
            return string.Empty;
        }
    }
}
