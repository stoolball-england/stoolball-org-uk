using System.Linq;

namespace Stoolball.Matches
{
    public class MatchResultEvaluator : IMatchResultEvaluator
    {
        /// <summary>
        /// Determine whether a match is a <see cref="MatchResultType.HomeWin"/>, <see cref="MatchResultType.AwayWin"/> or <see cref="MatchResultType.Tie"/> based on data in <paramref name="match"/>.
        /// </summary>
        /// <param name="match">A <see cref="Match"/> with its <see cref="Match.MatchInnings"/> and <see cref="Match.Teams"/> collections populated</param>
        public MatchResultType? EvaluateMatchResult(Match match)
        {
            if (match is null)
            {
                throw new System.ArgumentNullException(nameof(match));
            }

            if (match.MatchInnings != null && !match.MatchInnings.Any() || match.MatchInnings.Any(x => !x.Runs.HasValue)) return null;
            if (match.Teams != null && !match.Teams.Any() || match.Teams.Any(x => !x.MatchTeamId.HasValue)) return null;

            var totalRunsByTeam = match.MatchInnings.GroupBy(x => x.BattingMatchTeamId).Select(innings =>
            {
                return (MatchTeamId: innings.First().BattingMatchTeamId.Value, Runs: innings.Sum(x => x.Runs));
            });

            var teamsWithWinningScore = totalRunsByTeam.Where(x => x.Runs == totalRunsByTeam.Max(y => y.Runs)).ToList();

            if (teamsWithWinningScore.Count > 1)
            {
                return MatchResultType.Tie;
            }
            else if (teamsWithWinningScore.Count == 1)
            {
                var winningRole = match.Teams.Single(x => x.MatchTeamId == teamsWithWinningScore[0].MatchTeamId).TeamRole;
                return winningRole == TeamRole.Home ? MatchResultType.HomeWin : MatchResultType.AwayWin;
            }

            return null;
        }
    }
}
