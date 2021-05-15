using System.Threading.Tasks;

namespace Stoolball.Statistics
{
    public interface IPlayerSummaryStatisticsDataSource
    {
        Task<BattingStatistics> ReadBattingStatistics(StatisticsFilter filter);
        Task<BowlingStatistics> ReadBowlingStatistics(StatisticsFilter filter);
        Task<FieldingStatistics> ReadFieldingStatistics(StatisticsFilter filter);
    }
}