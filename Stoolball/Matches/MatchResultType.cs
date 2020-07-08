namespace Stoolball.Matches
{
    public enum MatchResultType
    {
        /// <summary>
        /// Home team won the game
        /// </summary>
        HomeWin,

        /// <summary>
        /// Away team won the game
        /// </summary>
        AwayWin,

        /// <summary>
        /// Home team won due to forfeit by away team
        /// </summary>
        HomeWinByForfeit,

        /// <summary>
        /// Away team won due to forfeit by home team
        /// </summary>
        AwayWinByForfeit,

        /// <summary>
        /// Both teams achieved the same score
        /// </summary>
        Tie,

        /// <summary>
        /// Match will be played at a different time
        /// </summary>
        Postponed,

        /// <summary>
        /// Match was cancelled before play
        /// </summary>
        Cancelled,

        /// <summary>
        /// Match was called off at the beginning of or during play and will be replayed
        /// </summary>
        AbandonedDuringPlayAndPostponed,

        /// <summary>
        /// Match was called off at the beginning of or during play and will not be replayed
        /// </summary>
        AbandonedDuringPlayAndCancelled
    }
}