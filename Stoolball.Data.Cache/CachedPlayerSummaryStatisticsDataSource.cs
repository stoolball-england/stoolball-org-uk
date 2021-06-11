using System.Threading.Tasks;
using Polly;
using Polly.Registry;
using Stoolball.Caching;
using Stoolball.Statistics;

namespace Stoolball.Data.Cache
{
    public class CachedPlayerSummaryStatisticsDataSource : IPlayerSummaryStatisticsDataSource
    {
        private readonly IReadOnlyPolicyRegistry<string> _policyRegistry;
        private readonly ICacheablePlayerSummaryStatisticsDataSource _playerSummaryStatisticsDataSource;
        private readonly IStatisticsFilterSerializer _statisticsFilterSerializer;

        public CachedPlayerSummaryStatisticsDataSource(IReadOnlyPolicyRegistry<string> policyRegistry, ICacheablePlayerSummaryStatisticsDataSource playerSummaryStatisticsDataSource, IStatisticsFilterSerializer statisticsFilterSerializer)
        {
            _policyRegistry = policyRegistry ?? throw new System.ArgumentNullException(nameof(policyRegistry));
            _playerSummaryStatisticsDataSource = playerSummaryStatisticsDataSource ?? throw new System.ArgumentNullException(nameof(playerSummaryStatisticsDataSource));
            _statisticsFilterSerializer = statisticsFilterSerializer ?? throw new System.ArgumentNullException(nameof(statisticsFilterSerializer));
        }

        public async Task<BattingStatistics> ReadBattingStatistics(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _playerSummaryStatisticsDataSource.ReadBattingStatistics(filter).ConfigureAwait(false), new Context(nameof(ReadBattingStatistics) + _statisticsFilterSerializer.Serialize(filter)));
        }

        public async Task<BowlingStatistics> ReadBowlingStatistics(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _playerSummaryStatisticsDataSource.ReadBowlingStatistics(filter).ConfigureAwait(false), new Context(nameof(ReadBowlingStatistics) + _statisticsFilterSerializer.Serialize(filter)));
        }

        public async Task<FieldingStatistics> ReadFieldingStatistics(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _playerSummaryStatisticsDataSource.ReadFieldingStatistics(filter).ConfigureAwait(false), new Context(nameof(ReadFieldingStatistics) + _statisticsFilterSerializer.Serialize(filter)));
        }
    }
}
