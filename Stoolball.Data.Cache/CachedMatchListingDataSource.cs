using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Registry;
using Stoolball.Data.SqlServer;
using Stoolball.Matches;

namespace Stoolball.Data.Cache
{
    public class CachedMatchListingDataSource : SqlServerMatchListingDataSource
    {
        private readonly IReadOnlyPolicyRegistry<string> _policyRegistry;
        private readonly IMatchFilterSerializer _matchFilterSerializer;

        public CachedMatchListingDataSource(IDatabaseConnectionFactory databaseConnectionFactory, IReadOnlyPolicyRegistry<string> policyRegistry, IMatchFilterSerializer matchFilterSerializer) : base(databaseConnectionFactory)
        {
            _policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
            _matchFilterSerializer = matchFilterSerializer ?? throw new ArgumentNullException(nameof(matchFilterSerializer));
        }

        public async override Task<List<MatchListing>> ReadMatchListings(MatchFilter filter, MatchSortOrder sortOrder)
        {
            filter = filter ?? new MatchFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.MatchesPolicy);
            var cacheKey = CacheConstants.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter) + sortOrder.ToString();
            return await cachePolicy.ExecuteAsync(async context => await base.ReadMatchListings(filter, sortOrder), new Context(cacheKey));
        }
    }
}
