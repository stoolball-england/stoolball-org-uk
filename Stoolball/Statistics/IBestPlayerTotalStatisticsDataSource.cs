using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stoolball.Statistics
{
    public interface IBestPlayerTotalStatisticsDataSource
    {
        Task<IEnumerable<StatisticsResult<BestTotal>>> ReadMostRunsScored(StatisticsFilter filter);
        Task<IEnumerable<StatisticsResult<BestTotal>>> ReadMostWickets(StatisticsFilter filter);
        Task<IEnumerable<StatisticsResult<BestTotal>>> ReadMostCatches(StatisticsFilter filter);
        Task<IEnumerable<StatisticsResult<BestTotal>>> ReadMostRunOuts(StatisticsFilter filter);
        Task<int> ReadTotalPlayersWithRunsScored(StatisticsFilter filter);
        Task<int> ReadTotalPlayersWithWickets(StatisticsFilter filter);
        Task<int> ReadTotalPlayersWithCatches(StatisticsFilter filter);
        Task<int> ReadTotalPlayersWithRunOuts(StatisticsFilter filter);
    }
}