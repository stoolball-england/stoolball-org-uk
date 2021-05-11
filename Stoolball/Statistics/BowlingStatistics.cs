namespace Stoolball.Statistics
{
    public class BowlingStatistics
    {
        public int TotalInnings { get; set; }
        public int TotalInningsWithRunsConceded { get; set; }
        public int TotalInningsWithBallsBowled { get; set; }
        public decimal TotalOvers { get; set; }
        public int TotalMaidens { get; set; }
        public int TotalRunsConceded { get; set; }
        public int TotalWickets { get; set; }
        public int FiveWicketInnings { get; set; }
        public int? BestInningsRunsConceded { get; set; }
        public int? BestInningsWickets { get; set; }
        public decimal? Economy { get; set; }
        public decimal? StrikeRate { get; set; }
        public decimal? Average { get; set; }
    }
}
