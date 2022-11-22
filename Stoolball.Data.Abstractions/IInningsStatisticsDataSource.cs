using System.Threading.Tasks;
using Stoolball.Statistics;

namespace Stoolball.Data.Abstractions
{
    public interface IInningsStatisticsDataSource
    {
        Task<InningsStatistics> ReadInningsStatistics(StatisticsFilter statisticsFilter);
    }
}