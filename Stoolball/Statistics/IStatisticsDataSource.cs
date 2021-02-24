using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stoolball.Statistics
{
    public interface IStatisticsDataSource
    {
        Task<IEnumerable<PlayerInningsResult>> ReadPlayerInnings(StatisticsFilter filter, StatisticsSortOrder sortOrder);
        Task<int> ReadTotalPlayerInnings(StatisticsFilter filter);
    }
}