using System.Threading.Tasks;

namespace Stoolball.Statistics
{
    public interface IPlayerSummaryStatisticsDataSource
    {
        Task<BattingStatistics> ReadBattingStatistics(StatisticsFilter statisticsFilter);
    }
}