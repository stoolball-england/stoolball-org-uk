using Stoolball.Teams;

namespace Stoolball.Matches
{
    public class MatchInnings
    {
        public int InningsOrderInMatch { get; set; }

        public Team Team { get; set; }

        public int? Overs { get; set; }

        public int? Runs { get; set; }

        public int? Wickets { get; set; }
    }
}
