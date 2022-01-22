using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Registry;
using Stoolball.Caching;
using Stoolball.MatchLocations;

namespace Stoolball.Data.Cache
{
    public class CachedMatchLocationDataSource : IMatchLocationDataSource
    {
        private readonly ICacheableMatchLocationDataSource _matchLocationDataSource;
        private readonly IReadOnlyPolicyRegistry<string> _policyRegistry;
        private readonly IMatchLocationFilterSerializer _matchLocationFilterSerializer;
        private readonly ICacheOverride _cacheOverride;
        private bool? _cacheDisabled;

        public CachedMatchLocationDataSource(IReadOnlyPolicyRegistry<string> policyRegistry, ICacheableMatchLocationDataSource matchLocationDataSource, IMatchLocationFilterSerializer matchLocationFilterSerializer, ICacheOverride cacheOverride)
        {
            _policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
            _matchLocationDataSource = matchLocationDataSource ?? throw new ArgumentNullException(nameof(matchLocationDataSource));
            _matchLocationFilterSerializer = matchLocationFilterSerializer ?? throw new ArgumentNullException(nameof(matchLocationFilterSerializer));
            _cacheOverride = cacheOverride ?? throw new ArgumentNullException(nameof(cacheOverride));
        }

        private async Task<bool> CacheDisabled()
        {
            if (!_cacheDisabled.HasValue)
            {
                _cacheDisabled = await _cacheOverride.IsCacheOverriddenForCurrentMember(CacheConstants.MatchLocationsCacheKeyPrefix).ConfigureAwait(false);
            }
            return _cacheDisabled.Value;
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalMatchLocations(MatchLocationFilter filter)
        {
            filter = filter ?? new MatchLocationFilter();

            if (await CacheDisabled().ConfigureAwait(false))
            {
                return await _matchLocationDataSource.ReadTotalMatchLocations(filter).ConfigureAwait(false);
            }
            else
            {
                var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.MatchLocationsPolicy);
                var cacheKey = CacheConstants.MatchLocationsCacheKeyPrefix + nameof(ReadTotalMatchLocations) + _matchLocationFilterSerializer.Serialize(filter);
                return await cachePolicy.ExecuteAsync(async context => await _matchLocationDataSource.ReadTotalMatchLocations(filter).ConfigureAwait(false), new Context(cacheKey));
            }
        }

        /// <inheritdoc />
        public async Task<List<MatchLocation>> ReadMatchLocations(MatchLocationFilter filter)
        {
            filter = filter ?? new MatchLocationFilter();

            if (await CacheDisabled().ConfigureAwait(false))
            {
                return await _matchLocationDataSource.ReadMatchLocations(filter).ConfigureAwait(false);
            }
            else
            {
                var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.MatchLocationsPolicy);
                var cacheKey = CacheConstants.MatchLocationsCacheKeyPrefix + nameof(ReadMatchLocations) + _matchLocationFilterSerializer.Serialize(filter);
                return await cachePolicy.ExecuteAsync(async context => await _matchLocationDataSource.ReadMatchLocations(filter).ConfigureAwait(false), new Context(cacheKey));
            }
        }

        public async Task<MatchLocation> ReadMatchLocationByRoute(string route, bool includeRelated = false)
        {
            return await _matchLocationDataSource.ReadMatchLocationByRoute(route, includeRelated).ConfigureAwait(false);
        }
    }
}
