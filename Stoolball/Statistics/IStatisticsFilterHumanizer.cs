namespace Stoolball.Statistics
{
    public interface IStatisticsFilterHumanizer
    {
        string MatchingFilter(StatisticsFilter filter);
        string StatisticsMatchingFilter(StatisticsFilter filter);
    }
}