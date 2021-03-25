namespace Stoolball.Matches
{
    /// <summary>
    /// Enum determining what kind of stoolball match is being represented
    /// </summary>
    public enum MatchType
    {
        /// <summary>
        /// A match in a league
        /// </summary>
        LeagueMatch,

        /// <summary>
        /// A training session, rather than a real match
        /// </summary>
        TrainingSession,

        /// <summary>
        /// A one-off friendly match
        /// </summary>
        FriendlyMatch,

        /// <summary>
        /// A knock-out cup match, in which the loser is usually eliminated from the competition
        /// </summary>
        KnockoutMatch
    }
}