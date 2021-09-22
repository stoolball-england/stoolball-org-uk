namespace Stoolball.Statistics
{
    public interface IStatisticsFilterQueryStringSerializer
    {
        string Serialize(StatisticsFilter filter);
        string Serialize(StatisticsFilter filter, StatisticsFilter defaultFilter);
    }
}