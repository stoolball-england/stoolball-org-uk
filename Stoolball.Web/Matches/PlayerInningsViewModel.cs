using Stoolball.Matches;

namespace Stoolball.Web.Matches
{
    public class PlayerInningsViewModel
    {
        public string Batter { get; set; }
        public DismissalType? DismissalType { get; set; }
        public string DismissedBy { get; set; }
        public string Bowler { get; set; }
        public int? RunsScored { get; set; }
        public int? BallsFaced { get; set; }
    }
}