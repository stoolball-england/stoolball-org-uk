using Stoolball.Teams;
using System.Collections.Generic;

namespace Stoolball.Matches
{
    public class MatchInnings
    {
        public int InningsOrderInMatch { get; set; }

        public Team Team { get; set; }

        public int? Overs { get; set; }

        public SortedList<int, Batting> Batting { get; internal set; } = new SortedList<int, Batting>();
        public SortedList<int, BowlingOver> BowlingOvers { get; internal set; } = new SortedList<int, BowlingOver>();

        public int? Runs { get; set; }

        public int? Wickets { get; set; }
    }
}
