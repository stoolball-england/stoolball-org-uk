using Stoolball.Matches;

namespace Stoolball.Web.Matches.Models
{
    public class ScorecardViewModel
    {
        public MatchInnings MatchInnings { get; set; } = new MatchInnings();
        public int TotalInningsInMatch { get; set; } = 2;
    }
}