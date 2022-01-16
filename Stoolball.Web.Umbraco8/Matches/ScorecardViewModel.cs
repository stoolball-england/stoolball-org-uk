using Stoolball.Matches;

namespace Stoolball.Web.Matches
{
    public class ScorecardViewModel
    {
        public MatchInnings MatchInnings { get; set; }
        public int TotalInningsInMatch { get; set; } = 2;
    }
}