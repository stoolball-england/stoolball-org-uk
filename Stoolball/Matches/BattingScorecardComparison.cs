using System.Collections.Generic;

namespace Stoolball.Matches
{
    public class BattingScorecardComparison
    {
        public List<PlayerInnings> PlayerInningsAdded { get; internal set; } = new List<PlayerInnings>();
        public List<(PlayerInnings, PlayerInnings)> PlayerInningsChanged { get; internal set; } = new List<(PlayerInnings, PlayerInnings)>();
        public List<PlayerInnings> PlayerInningsRemoved { get; internal set; } = new List<PlayerInnings>();
        public List<PlayerInnings> PlayerInningsUnchanged { get; internal set; } = new List<PlayerInnings>();
        public List<string> BattingPlayerIdentitiesAdded { get; internal set; } = new List<string>();
        public List<string> BattingPlayerIdentitiesAffected { get; internal set; } = new List<string>();
        public List<string> BattingPlayerIdentitiesRemoved { get; internal set; } = new List<string>();
        public List<string> BowlingPlayerIdentitiesAdded { get; internal set; } = new List<string>();
        public List<string> BowlingPlayerIdentitiesAffected { get; internal set; } = new List<string>();
        public List<string> BowlingPlayerIdentitiesRemoved { get; internal set; } = new List<string>();
    }
}