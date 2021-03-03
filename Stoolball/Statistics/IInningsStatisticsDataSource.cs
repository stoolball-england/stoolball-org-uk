using System.Threading.Tasks;

namespace Stoolball.Statistics
{
    public interface IInningsStatisticsDataSource
    {
        Task<InningsStatistics> ReadInningsStatistics(StatisticsFilter statisticsFilter);
    }
}