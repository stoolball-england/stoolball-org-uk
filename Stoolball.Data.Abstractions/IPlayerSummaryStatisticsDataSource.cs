using System.Threading.Tasks;
using Stoolball.Statistics;

namespace Stoolball.Data.Abstractions
{
    public interface IPlayerSummaryStatisticsDataSource
    {
        Task<BattingStatistics> ReadBattingStatistics(StatisticsFilter filter);
        Task<BowlingStatistics> ReadBowlingStatistics(StatisticsFilter filter);
        Task<FieldingStatistics> ReadFieldingStatistics(StatisticsFilter filter);
    }
}