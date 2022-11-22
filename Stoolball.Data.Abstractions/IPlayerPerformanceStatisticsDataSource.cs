using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Matches;
using Stoolball.Statistics;

namespace Stoolball.Data.Abstractions
{
    public interface IPlayerPerformanceStatisticsDataSource
    {
        Task<IEnumerable<StatisticsResult<PlayerInnings>>> ReadPlayerInnings(StatisticsFilter filter);
    }
}