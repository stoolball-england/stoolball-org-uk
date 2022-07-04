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
            var cacheKey = nameof(IPlayerDataSource.ReadPlayerByRoute) + cacheable.PlayerRoute;
            _readThroughCache.InvalidateCache(cacheKey);
            return Task.CompletedTask;
        }
    }
}
