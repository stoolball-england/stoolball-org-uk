namespace Stoolball.Statistics
{
    public interface IStatisticsFilterHumanizer
    {
        string MatchingUserFilter(StatisticsFilter filter);
        string MatchingFixedFilter(StatisticsFilter filter);
        string StatisticsMatchingUserFilter(StatisticsFilter filter);
    }
}