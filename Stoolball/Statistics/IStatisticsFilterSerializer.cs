namespace Stoolball.Statistics
{
    public interface IStatisticsFilterSerializer
    {
        string Serialize(StatisticsFilter filter);
    }
}