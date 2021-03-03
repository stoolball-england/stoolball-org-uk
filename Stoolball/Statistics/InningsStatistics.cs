namespace Stoolball.Statistics
{
    public class InningsStatistics
    {
        public decimal? AverageRunsScored { get; set; }
        public int? HighestRunsScored { get; set; }
        public int? LowestRunsScored { get; set; }
        public decimal? AverageRunsConceded { get; set; }
        public int? HighestRunsConceded { get; set; }
        public int? LowestRunsConceded { get; set; }
        public decimal? AverageWicketsLost { get; set; }
        public decimal? AverageWicketsTaken { get; set; }

        public bool AnyRunsScored()
        {
            return AverageRunsScored.HasValue ||
                HighestRunsScored.HasValue ||
                LowestRunsScored.HasValue;
        }

        public bool AnyRunsConceded()
        {
            return AverageRunsConceded.HasValue ||
                HighestRunsConceded.HasValue ||
                LowestRunsConceded.HasValue;
        }

        public bool AnyWickets()
        {
            return AverageWicketsLost.HasValue ||
                AverageWicketsTaken.HasValue;
        }

        public bool Any()
        {
            return AnyRunsScored() ||
                AnyRunsConceded() ||
                AnyWickets();
        }
    }
}