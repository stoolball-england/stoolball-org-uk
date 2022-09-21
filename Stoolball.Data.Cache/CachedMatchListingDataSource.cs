using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Caching;
using Stoolball.Matches;

namespace Stoolball.Data.Cache
{
    public class CachedMatchListingDataSource : IMatchListingDataSource
    {
        private readonly IReadThroughCache _readThroughCache;
        private readonly ICacheableMatchListingDataSource _matchListingDataSource;
        private readonly IMatchFilterQueryStringSerializer _matchFilterSerializer;

        public CachedMatchListingDataSource(IReadThroughCache readThroughCache, ICacheableMatchListingDataSource matchListingDataSource, IMatchFilterQueryStringSerializer matchFilterSerializer)
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
            _matchListingDataSource = matchListingDataSource ?? throw new ArgumentNullException(nameof(matchListingDataSource));
            _matchFilterSerializer = matchFilterSerializer ?? throw new ArgumentNullException(nameof(matchFilterSerializer));
        }

        /// <inheritdoc />
        public async Task<List<MatchListing>> ReadMatchListings(MatchFilter filter, MatchSortOrder sortOrder)
        {
            filter = filter ?? new MatchFilter();
            var cacheKey = nameof(IMatchListingDataSource) + nameof(ReadMatchListings);
            var dependentCacheKey = cacheKey + _matchFilterSerializer.Serialize(filter) + sortOrder.ToString();
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _matchListingDataSource.ReadMatchListings(filter, sortOrder), CachePolicy.MatchesExpiration(), cacheKey, dependentCacheKey);
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalMatches(MatchFilter filter)
        {
            var cacheKey = nameof(IMatchListingDataSource) + nameof(ReadTotalMatches);
            var dependentCacheKey = cacheKey + _matchFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _matchListingDataSource.ReadTotalMatches(filter).ConfigureAwait(false), CachePolicy.MatchesExpiration(), cacheKey, dependentCacheKey);
        }
    }
}
