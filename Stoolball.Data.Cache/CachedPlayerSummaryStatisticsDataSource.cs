using System.Threading.Tasks;
using Stoolball.Caching;
using Stoolball.Statistics;

namespace Stoolball.Data.Cache
{
    public class CachedPlayerSummaryStatisticsDataSource : IPlayerSummaryStatisticsDataSource
    {
        private readonly IReadThroughCache _readThroughCache;
        private readonly ICacheablePlayerSummaryStatisticsDataSource _playerSummaryStatisticsDataSource;
        private readonly IStatisticsFilterQueryStringSerializer _statisticsFilterSerializer;

        public CachedPlayerSummaryStatisticsDataSource(IReadThroughCache readThroughCache, ICacheablePlayerSummaryStatisticsDataSource playerSummaryStatisticsDataSource, IStatisticsFilterQueryStringSerializer statisticsFilterSerializer)
        {
            _readThroughCache = readThroughCache ?? throw new System.ArgumentNullException(nameof(readThroughCache));
            _playerSummaryStatisticsDataSource = playerSummaryStatisticsDataSource ?? throw new System.ArgumentNullException(nameof(playerSummaryStatisticsDataSource));
            _statisticsFilterSerializer = statisticsFilterSerializer ?? throw new System.ArgumentNullException(nameof(statisticsFilterSerializer));
        }

        public async Task<BattingStatistics> ReadBattingStatistics(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var dependentCacheKey = nameof(IPlayerSummaryStatisticsDataSource) + nameof(ReadBattingStatistics) + _statisticsFilterSerializer.Serialize(filter);
            var cacheKey = string.IsNullOrEmpty(filter.Player?.PlayerRoute) ? dependentCacheKey : nameof(IPlayerSummaryStatisticsDataSource) + nameof(ReadBattingStatistics) + filter.Player.PlayerRoute;
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _playerSummaryStatisticsDataSource.ReadBattingStatistics(filter).ConfigureAwait(false), CacheConstants.StatisticsExpiration(), cacheKey, dependentCacheKey);
        }

        public async Task<BowlingStatistics> ReadBowlingStatistics(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var dependentCacheKey = nameof(IPlayerSummaryStatisticsDataSource) + nameof(ReadBowlingStatistics) + _statisticsFilterSerializer.Serialize(filter);
            var cacheKey = string.IsNullOrEmpty(filter.Player?.PlayerRoute) ? dependentCacheKey : nameof(IPlayerSummaryStatisticsDataSource) + nameof(ReadBowlingStatistics) + filter.Player.PlayerRoute;
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _playerSummaryStatisticsDataSource.ReadBowlingStatistics(filter).ConfigureAwait(false), CacheConstants.StatisticsExpiration(), cacheKey, dependentCacheKey);
        }

        public async Task<FieldingStatistics> ReadFieldingStatistics(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var dependentCacheKey = nameof(IPlayerSummaryStatisticsDataSource) + nameof(ReadFieldingStatistics) + _statisticsFilterSerializer.Serialize(filter);
            var cacheKey = string.IsNullOrEmpty(filter.Player?.PlayerRoute) ? dependentCacheKey : nameof(IPlayerSummaryStatisticsDataSource) + nameof(ReadFieldingStatistics) + filter.Player.PlayerRoute;
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _playerSummaryStatisticsDataSource.ReadFieldingStatistics(filter).ConfigureAwait(false), CacheConstants.StatisticsExpiration(), cacheKey, dependentCacheKey);
        }
    }
}
