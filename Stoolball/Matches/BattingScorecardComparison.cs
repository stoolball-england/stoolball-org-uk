using System.Collections.Generic;

namespace Stoolball.Matches
{
    public class BattingScorecardComparison
    {
        public List<PlayerInnings> PlayerInningsAdded { get; internal set; } = new List<PlayerInnings>();
        public List<(PlayerInnings, PlayerInnings)> PlayerInningsChanged { get; internal set; } = new List<(PlayerInnings, PlayerInnings)>();
        public List<PlayerInnings> PlayerInningsRemoved { get; internal set; } = new List<PlayerInnings>();
        public List<PlayerInnings> PlayerInningsUnchanged { get; internal set; } = new List<PlayerInnings>();

        /// <summary>
        /// Batter identities which were not featured in the 'before' scorecard.
        /// </summary>
        public List<string> BattingPlayerIdentitiesAdded { get; internal set; } = new List<string>();

        /// <summary>
        /// Batter identities which were added, removed, or had their innings details changed in the 'after' scorecard.
        /// </summary>
        public List<string> BattingPlayerIdentitiesAffected { get; internal set; } = new List<string>();

        /// <summary>
        /// Batter identities which were not featured in the 'after' scorecard.
        /// </summary>
        public List<string> BattingPlayerIdentitiesRemoved { get; internal set; } = new List<string>();

        /// <summary>
        /// Bowler and fielder identities which were not featured in the 'before' scorecard.
        /// </summary>
        public List<string> BowlingPlayerIdentitiesAdded { get; internal set; } = new List<string>();

        /// <summary>
        /// Bowler and fielder identities which were added, removed, or had wickets attributed to them changed in the 'after' scorecard.
        /// </summary>
        public List<string> BowlingPlayerIdentitiesAffected { get; internal set; } = new List<string>();

        /// <summary>
        /// Bowler and fielder identities which were not featured in the 'after' scorecard.
        /// </summary>
        public List<string> BowlingPlayerIdentitiesRemoved { get; internal set; } = new List<string>();
    }
}