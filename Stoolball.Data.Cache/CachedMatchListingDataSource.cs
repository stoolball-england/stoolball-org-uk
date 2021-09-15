using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Registry;
using Stoolball.Caching;
using Stoolball.Matches;

namespace Stoolball.Data.Cache
{
    public class CachedMatchListingDataSource : IMatchListingDataSource
    {
        private readonly ICacheableMatchListingDataSource _matchListingDataSource;
        private readonly IReadOnlyPolicyRegistry<string> _policyRegistry;
        private readonly IMatchFilterQueryStringSerializer _matchFilterSerializer;

        public CachedMatchListingDataSource(IReadOnlyPolicyRegistry<string> policyRegistry, ICacheableMatchListingDataSource matchListingDataSource, IMatchFilterQueryStringSerializer matchFilterSerializer)
        {
            _policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
            _matchListingDataSource = matchListingDataSource ?? throw new ArgumentNullException(nameof(matchListingDataSource));
            _matchFilterSerializer = matchFilterSerializer ?? throw new ArgumentNullException(nameof(matchFilterSerializer));
        }

        /// <inheritdoc />
        public async Task<List<MatchListing>> ReadMatchListings(MatchFilter filter, MatchSortOrder sortOrder)
        {
            filter = filter ?? new MatchFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.MatchesPolicy);
            var cacheKey = CacheConstants.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter) + sortOrder.ToString();
            return await cachePolicy.ExecuteAsync(async context => await _matchListingDataSource.ReadMatchListings(filter, sortOrder), new Context(cacheKey));
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalMatches(MatchFilter filter)
        {
            return await _matchListingDataSource.ReadTotalMatches(filter).ConfigureAwait(false);
        }
    }
}
