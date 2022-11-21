using System;
using Stoolball.Data.Abstractions;
using Stoolball.MatchLocations;

namespace Stoolball.Data.MemoryCache
{
    public class MatchLocationListingCacheInvalidator : IListingCacheInvalidator<MatchLocation>
    {
        private readonly IReadThroughCache _readThroughCache;

        public MatchLocationListingCacheInvalidator(IReadThroughCache readThroughCache)
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
        }

        public void InvalidateCache()
        {
            _readThroughCache.InvalidateCache(nameof(IMatchLocationDataSource) + nameof(IMatchLocationDataSource.ReadTotalMatchLocations));
            _readThroughCache.InvalidateCache(nameof(IMatchLocationDataSource) + nameof(IMatchLocationDataSource.ReadMatchLocations));
        }
    }
}
