using System;
using System.Threading.Tasks;
using Stoolball.Caching;

namespace Stoolball.Statistics
{
    public class PlayerCacheClearer : ICacheClearer<Player>
    {
        private readonly IReadThroughCache _readThroughCache;

        public PlayerCacheClearer(IReadThroughCache readThroughCache)
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
        }

        public Task ClearCacheFor(Player cacheable)
        {
            if (cacheable is null)
            {
                throw new ArgumentNullException(nameof(cacheable));
            }

            if (string.IsNullOrEmpty(cacheable.PlayerRoute))
            {
                throw new ArgumentException($"{nameof(cacheable.PlayerRoute)} cannot be null or empty string");
            }

            _readThroughCache.InvalidateCache(nameof(IPlayerDataSource) + nameof(IPlayerDataSource.ReadPlayerByRoute) + cacheable.PlayerRoute);
            if (cacheable.MemberKey.HasValue)
            {
                _readThroughCache.InvalidateCache(nameof(IPlayerDataSource) + nameof(IPlayerDataSource.ReadPlayerByMemberKey) + cacheable.MemberKey);
            }
            _readThroughCache.InvalidateCache(nameof(IPlayerSummaryStatisticsDataSource) + nameof(IPlayerSummaryStatisticsDataSource.ReadBattingStatistics) + cacheable.PlayerRoute);
            _readThroughCache.InvalidateCache(nameof(IPlayerSummaryStatisticsDataSource) + nameof(IPlayerSummaryStatisticsDataSource.ReadBowlingStatistics) + cacheable.PlayerRoute);
            _readThroughCache.InvalidateCache(nameof(IPlayerSummaryStatisticsDataSource) + nameof(IPlayerSummaryStatisticsDataSource.ReadFieldingStatistics) + cacheable.PlayerRoute);

            return Task.CompletedTask;
        }
    }
}
