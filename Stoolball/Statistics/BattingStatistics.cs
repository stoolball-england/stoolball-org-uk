namespace Stoolball.Statistics
{
    public class BattingStatistics
    {
        public int TotalInnings { get; set; }
        public int TotalInningsWithRunsScored { get; set; }
        public int TotalInningsWithRunsScoredAndBallsFaced { get; set; }
        public int NotOuts { get; set; }
        public int TotalRunsScored { get; set; }
        public int Fifties { get; set; }
        public int Hundreds { get; set; }
        public int? BestInningsRunsScored { get; set; }
        public bool? BestInningsWasDismissed { get; set; }
        public decimal? StrikeRate { get; set; }
        public decimal? Average { get; set; }
    }
}
