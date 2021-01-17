using Stoolball.Teams;

namespace Stoolball.Statistics
{
    public class BowlingFigures
    {
        public PlayerIdentity Bowler { get; set; }
        public double? Overs { get; set; }
        public int? Maidens { get; set; }
        public int? RunsConceded { get; set; }
        public int Wickets { get; set; }
    }
}
