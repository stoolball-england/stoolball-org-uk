using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Statistics;

namespace Stoolball.Data.Abstractions
{
    public interface IBestPlayerAverageStatisticsDataSource
    {
        /// <summary>
        /// Reads players matching the supplied filter and their batting averages, sorted with the players with the best averages first
        /// </summary>
        Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestBattingAverage(StatisticsFilter filter);

        /// <summary>
        /// Reads players matching the supplied filter and their bowling averages, sorted with the players with the best averages first
        /// </summary>
        Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestBowlingAverage(StatisticsFilter filter);

        /// <summary>
        /// Reads players matching the supplied filter and their bowling economy rates, sorted with the players with the best economy rate first
        /// </summary>
        Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestEconomyRate(StatisticsFilter filter);

        /// <summary>
        /// Reads players matching the supplied filter and their batting strike rates, sorted with the players with the best strike rate first
        /// </summary>
        Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestBattingStrikeRate(StatisticsFilter filter);

        /// <summary>
        /// Reads players matching the supplied filter and their bowling strike rates, sorted with the players with the best strike rate first
        /// </summary>
        Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestBowlingStrikeRate(StatisticsFilter filter);

        /// <summary>
        /// Reads how many players matching the supplied filter have enough data to calculate a batting average.
        /// </summary>
        Task<int> ReadTotalPlayersWithBattingAverage(StatisticsFilter filter);

        /// <summary>
        /// Reads how many players matching the supplied filter have enough data to calculate a bowling average.
        /// </summary>
        Task<int> ReadTotalPlayersWithBowlingAverage(StatisticsFilter filter);

        /// <summary>
        /// Reads how many players matching the supplied filter have enough data to calculate a bowling economy rate.
        /// </summary>
        Task<int> ReadTotalPlayersWithEconomyRate(StatisticsFilter filter);

        /// <summary>
        /// Reads how many players matching the supplied filter have enough data to calculate a batting strike rate.
        /// </summary>
        Task<int> ReadTotalPlayersWithBattingStrikeRate(StatisticsFilter filter);

        /// <summary>
        /// Reads how many players matching the supplied filter have enough data to calculate a bowling strike rate.
        /// </summary>
        Task<int> ReadTotalPlayersWithBowlingStrikeRate(StatisticsFilter filter);
    }
}