using System;
using Stoolball.Caching;

namespace Stoolball.MatchLocations
{
    public class MatchLocationListingCacheClearer : IListingCacheClearer<MatchLocation>
    {
        private readonly IReadThroughCache _readThroughCache;

        public MatchLocationListingCacheClearer(IReadThroughCache readThroughCache)
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
        }

        public void ClearCache()
        {
            _readThroughCache.InvalidateCache(nameof(IMatchLocationDataSource) + nameof(IMatchLocationDataSource.ReadTotalMatchLocations));
            _readThroughCache.InvalidateCache(nameof(IMatchLocationDataSource) + nameof(IMatchLocationDataSource.ReadMatchLocations));
        }
    }
}
