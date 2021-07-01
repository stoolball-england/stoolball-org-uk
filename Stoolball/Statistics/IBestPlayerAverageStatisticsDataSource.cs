using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stoolball.Statistics
{
    public interface IBestPlayerAverageStatisticsDataSource
    {
        /// <summary>
        /// Reads players matching the supplied filter and their batting averages, sorted with the players with the best averages first
        /// </summary>
        Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestBattingAverage(StatisticsFilter filter);

        /// <summary>
        /// Reads how many players matching the supplied filter have enough data to calculate a batting average.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task<int> ReadTotalPlayersWithBattingAverage(StatisticsFilter filter);
    }
}