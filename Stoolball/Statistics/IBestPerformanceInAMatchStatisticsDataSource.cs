using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Matches;

namespace Stoolball.Statistics
{
    public interface IBestPerformanceInAMatchStatisticsDataSource
    {
        Task<IEnumerable<StatisticsResult<PlayerInnings>>> ReadPlayerInnings(StatisticsFilter filter, StatisticsSortOrder sortOrder);
        Task<int> ReadTotalPlayerInnings(StatisticsFilter filter);
        Task<IEnumerable<StatisticsResult<BowlingFigures>>> ReadBowlingFigures(StatisticsFilter filter, StatisticsSortOrder sortOrder);
        Task<int> ReadTotalBowlingFigures(StatisticsFilter filter);
    }
}