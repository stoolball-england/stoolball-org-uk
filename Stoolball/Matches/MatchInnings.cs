using Stoolball.Teams;
using System;
using System.Collections.Generic;

namespace Stoolball.Matches
{
    public class MatchInnings
    {
        public Guid? MatchInningsId { get; set; }
        public int InningsOrderInMatch { get; set; }

        public Team Team { get; set; }

        public int? Overs { get; set; }

        public List<PlayerInnings> PlayerInnings { get; internal set; } = new List<PlayerInnings>();
        public List<Over> OversBowled { get; internal set; } = new List<Over>();

        public int? Runs { get; set; }

        public int? Wickets { get; set; }
    }
}
