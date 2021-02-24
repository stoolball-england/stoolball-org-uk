using Stoolball.Matches;

namespace Stoolball.Statistics
{
    public class BattingPerformanceResult : BaseStatisticsResult
    {
        public DismissalType? DismissalType { get; set; }
        public int RunsScored { get; set; }
        public int? BallsFaced { get; set; }
    }
}
