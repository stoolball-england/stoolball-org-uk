namespace Stoolball.Matches
{
    public interface IMatchResultEvaluator
    {
        /// <summary>
        /// Determine whether a match is a <see cref="MatchResultType.HomeWin"/>, <see cref="MatchResultType.AwayWin"/> or <see cref="MatchResultType.Tie"/> based on data in <paramref name="match"/>.
        /// </summary>
        /// <param name="match">A <see cref="Match"/> with its <see cref="Match.MatchInnings"/> and <see cref="Match.Teams"/> collections populated</param>
        MatchResultType? EvaluateMatchResult(Match match);
    }
}