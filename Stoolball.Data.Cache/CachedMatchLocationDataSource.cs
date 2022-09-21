using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Caching;
using Stoolball.MatchLocations;

namespace Stoolball.Data.Cache
{
    public class CachedMatchLocationDataSource : IMatchLocationDataSource
    {
        private readonly IReadThroughCache _readThroughCache;
        private readonly ICacheableMatchLocationDataSource _matchLocationDataSource;
        private readonly IMatchLocationFilterSerializer _matchLocationFilterSerializer;

        public CachedMatchLocationDataSource(IReadThroughCache readThroughCache, ICacheableMatchLocationDataSource matchLocationDataSource, IMatchLocationFilterSerializer matchLocationFilterSerializer)
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
            _matchLocationDataSource = matchLocationDataSource ?? throw new ArgumentNullException(nameof(matchLocationDataSource));
            _matchLocationFilterSerializer = matchLocationFilterSerializer ?? throw new ArgumentNullException(nameof(matchLocationFilterSerializer));
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalMatchLocations(MatchLocationFilter filter)
        {
            filter = filter ?? new MatchLocationFilter();

            var cacheKey = nameof(IMatchLocationDataSource) + nameof(ReadTotalMatchLocations);
            var dependentCacheKey = cacheKey + _matchLocationFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _matchLocationDataSource.ReadTotalMatchLocations(filter).ConfigureAwait(false), CachePolicy.MatchLocationsExpiration(), cacheKey, dependentCacheKey);
        }

        /// <inheritdoc />
        public async Task<List<MatchLocation>> ReadMatchLocations(MatchLocationFilter filter)
        {
            filter = filter ?? new MatchLocationFilter();

            var cacheKey = nameof(IMatchLocationDataSource) + nameof(ReadMatchLocations);
            var dependentCacheKey = cacheKey + _matchLocationFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _matchLocationDataSource.ReadMatchLocations(filter).ConfigureAwait(false), CachePolicy.MatchLocationsExpiration(), cacheKey, dependentCacheKey);
        }

        public async Task<MatchLocation> ReadMatchLocationByRoute(string route, bool includeRelated = false)
        {
            return await _matchLocationDataSource.ReadMatchLocationByRoute(route, includeRelated).ConfigureAwait(false);
        }
    }
}
