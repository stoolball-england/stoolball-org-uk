using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stoolball.Statistics
{
    public interface IBestPlayerTotalStatisticsDataSource
    {
        Task<IEnumerable<StatisticsResult<BestTotal>>> ReadMostRunsScored(StatisticsFilter filter);
        Task<int> ReadTotalPlayersWithRunsScored(StatisticsFilter filter);
    }
}