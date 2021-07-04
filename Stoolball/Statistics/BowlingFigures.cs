using System;
using Stoolball.Matches;

namespace Stoolball.Statistics
{
    public class BowlingFigures
    {
        public Guid? BowlingFiguresId { get; set; }
        public MatchInnings MatchInnings { get; set; }
        public PlayerIdentity Bowler { get; set; }
        public decimal? Overs { get; set; }
        public int? Maidens { get; set; }
        public int? RunsConceded { get; set; }
        public int Wickets { get; set; }
    }
}
