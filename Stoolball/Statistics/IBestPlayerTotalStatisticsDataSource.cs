using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stoolball.Statistics
{
    public interface IBestPlayerTotalStatisticsDataSource
    {
        Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostRunsScored(StatisticsFilter filter);
        Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostWickets(StatisticsFilter filter);
        Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostCatches(StatisticsFilter filter);
        Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostRunOuts(StatisticsFilter filter);
        Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostPlayerOfTheMatchAwards(StatisticsFilter filter);

        Task<int> ReadTotalPlayersWithRunsScored(StatisticsFilter filter);
        Task<int> ReadTotalPlayersWithWickets(StatisticsFilter filter);
        Task<int> ReadTotalPlayersWithCatches(StatisticsFilter filter);
        Task<int> ReadTotalPlayersWithRunOuts(StatisticsFilter filter);
        Task<int> ReadTotalPlayersWithPlayerOfTheMatchAwards(StatisticsFilter filter);
    }
}