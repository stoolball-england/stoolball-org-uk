using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Matches;

namespace Stoolball.Statistics
{
    public interface IPlayerPerformanceStatisticsDataSource
    {
        Task<IEnumerable<StatisticsResult<PlayerInnings>>> ReadPlayerInnings(StatisticsFilter filter);
    }
}