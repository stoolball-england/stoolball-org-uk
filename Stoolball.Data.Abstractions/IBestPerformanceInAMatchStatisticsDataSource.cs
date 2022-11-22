using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Matches;
using Stoolball.Statistics;

namespace Stoolball.Data.Abstractions
{
    public interface IBestPerformanceInAMatchStatisticsDataSource
    {
        Task<IEnumerable<StatisticsResult<PlayerInnings>>> ReadPlayerInnings(StatisticsFilter filter, StatisticsSortOrder sortOrder);
        Task<int> ReadTotalPlayerInnings(StatisticsFilter filter);
        Task<IEnumerable<StatisticsResult<BowlingFigures>>> ReadBowlingFigures(StatisticsFilter filter, StatisticsSortOrder sortOrder);
        Task<int> ReadTotalBowlingFigures(StatisticsFilter filter);
        Task<IEnumerable<StatisticsResult<PlayerIdentityPerformance>>> ReadPlayerIdentityPerformances(StatisticsFilter filter);
        Task<int> ReadTotalPlayerIdentityPerformances(StatisticsFilter filter);
    }
}