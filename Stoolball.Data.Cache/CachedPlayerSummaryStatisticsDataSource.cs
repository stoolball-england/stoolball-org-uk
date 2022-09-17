using System;
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
            ThrowExceptionIfNoPlayerRoute(filter);

            var cacheKey = nameof(IPlayerSummaryStatisticsDataSource) + nameof(ReadBattingStatistics) + filter.Player.PlayerRoute;
            var dependentCacheKey = nameof(IPlayerSummaryStatisticsDataSource) + nameof(ReadBattingStatistics) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _playerSummaryStatisticsDataSource.ReadBattingStatistics(filter).ConfigureAwait(false), CacheConstants.StatisticsExpiration(), cacheKey, dependentCacheKey);
        }

        public async Task<BowlingStatistics> ReadBowlingStatistics(StatisticsFilter filter)
        {
            ThrowExceptionIfNoPlayerRoute(filter);

            var cacheKey = nameof(IPlayerSummaryStatisticsDataSource) + nameof(ReadBowlingStatistics) + filter.Player.PlayerRoute;
            var dependentCacheKey = nameof(IPlayerSummaryStatisticsDataSource) + nameof(ReadBowlingStatistics) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _playerSummaryStatisticsDataSource.ReadBowlingStatistics(filter).ConfigureAwait(false), CacheConstants.StatisticsExpiration(), cacheKey, dependentCacheKey);
        }

        public async Task<FieldingStatistics> ReadFieldingStatistics(StatisticsFilter filter)
        {
            ThrowExceptionIfNoPlayerRoute(filter);

            var cacheKey = nameof(IPlayerSummaryStatisticsDataSource) + nameof(ReadFieldingStatistics) + filter.Player.PlayerRoute;
            var dependentCacheKey = nameof(IPlayerSummaryStatisticsDataSource) + nameof(ReadFieldingStatistics) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _playerSummaryStatisticsDataSource.ReadFieldingStatistics(filter).ConfigureAwait(false), CacheConstants.StatisticsExpiration(), cacheKey, dependentCacheKey);
        }

        private static void ThrowExceptionIfNoPlayerRoute(StatisticsFilter filter)
        {
            if (filter is null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            if (filter.Player is null)
            {
                throw new ArgumentException($"The {nameof(filter.Player)} property of {nameof(filter)} cannot be null");
            }

            if (string.IsNullOrEmpty(filter.Player.PlayerRoute))
            {
                throw new ArgumentException($"The {nameof(filter.Player.PlayerRoute)} property of {nameof(filter)}.{nameof(filter.Player)} cannot be null or empty");
            }
        }
    }
}
