namespace Stoolball.Statistics
{
    public class BestStatistic
    {
        public Player Player { get; set; }
        public int TotalMatches { get; set; }
        public int? TotalInnings { get; set; }
        public int? Total { get; set; }
        public decimal? Average { get; set; }
    }
}
