using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Registry;
using Stoolball.Caching;
using Stoolball.Teams;

namespace Stoolball.Data.Cache
{
    public class CachedTeamListingDataSource : ITeamListingDataSource
    {
        private readonly ICacheableTeamListingDataSource _teamListingDataSource;
        private readonly IReadOnlyPolicyRegistry<string> _policyRegistry;
        private readonly ITeamListingFilterSerializer _teamListingFilterSerializer;
        private readonly ICacheOverride _cacheOverride;
        private bool? _cacheDisabled;

        public CachedTeamListingDataSource(IReadOnlyPolicyRegistry<string> policyRegistry, ICacheableTeamListingDataSource teamListingDataSource, ITeamListingFilterSerializer teamListingFilterSerializer, ICacheOverride cacheOverride)
        {
            _policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
            _teamListingDataSource = teamListingDataSource ?? throw new ArgumentNullException(nameof(teamListingDataSource));
            _teamListingFilterSerializer = teamListingFilterSerializer ?? throw new ArgumentNullException(nameof(teamListingFilterSerializer));
            _cacheOverride = cacheOverride ?? throw new ArgumentNullException(nameof(cacheOverride));
        }

        private bool CacheDisabled()
        {
            if (!_cacheDisabled.HasValue)
            {
                _cacheDisabled = _cacheOverride.IsCacheOverriddenForCurrentMember(CacheConstants.TeamListingsCacheKeyPrefix);
            }
            return _cacheDisabled.Value;
        }

        /// <inheritdoc />
        public async Task<List<TeamListing>> ReadTeamListings(TeamListingFilter filter)
        {
            filter = filter ?? new TeamListingFilter();

            if (CacheDisabled())
            {
                return await _teamListingDataSource.ReadTeamListings(filter).ConfigureAwait(false);
            }
            else
            {
                var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.TeamsPolicy);
                var cacheKey = CacheConstants.TeamListingsCacheKeyPrefix + nameof(ReadTeamListings) + _teamListingFilterSerializer.Serialize(filter);
                return await cachePolicy.ExecuteAsync(async context => await _teamListingDataSource.ReadTeamListings(filter).ConfigureAwait(false), new Context(cacheKey));
            }
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalTeams(TeamListingFilter filter)
        {
            filter = filter ?? new TeamListingFilter();

            if (CacheDisabled())
            {
                return await _teamListingDataSource.ReadTotalTeams(filter).ConfigureAwait(false);
            }
            else
            {
                var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.TeamsPolicy);
                var cacheKey = CacheConstants.TeamListingsCacheKeyPrefix + nameof(ReadTotalTeams) + _teamListingFilterSerializer.Serialize(filter);
                return await cachePolicy.ExecuteAsync(async context => await _teamListingDataSource.ReadTotalTeams(filter).ConfigureAwait(false), new Context(cacheKey));
            }
        }
    }
}
