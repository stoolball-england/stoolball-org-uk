using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Caching;
using Stoolball.Teams;

namespace Stoolball.Data.Cache
{
    public class CachedTeamListingDataSource : ITeamListingDataSource
    {
        private readonly IReadThroughCache _readThroughCache;
        private readonly ICacheableTeamListingDataSource _teamListingDataSource;
        private readonly ITeamListingFilterSerializer _teamListingFilterSerializer;

        public CachedTeamListingDataSource(IReadThroughCache readThroughCache, ICacheableTeamListingDataSource teamListingDataSource, ITeamListingFilterSerializer teamListingFilterSerializer)
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
            _teamListingDataSource = teamListingDataSource ?? throw new ArgumentNullException(nameof(teamListingDataSource));
            _teamListingFilterSerializer = teamListingFilterSerializer ?? throw new ArgumentNullException(nameof(teamListingFilterSerializer));
        }

        /// <inheritdoc />
        public async Task<List<TeamListing>> ReadTeamListings(TeamListingFilter filter)
        {
            filter = filter ?? new TeamListingFilter();

            var cacheKey = nameof(ITeamListingDataSource) + nameof(ReadTeamListings);
            var dependentCacheKey = cacheKey + _teamListingFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _teamListingDataSource.ReadTeamListings(filter).ConfigureAwait(false), CacheConstants.TeamsExpiration(), cacheKey, dependentCacheKey);
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalTeams(TeamListingFilter filter)
        {
            filter = filter ?? new TeamListingFilter();

            var cacheKey = nameof(ITeamListingDataSource) + nameof(ReadTotalTeams);
            var dependentCacheKey = cacheKey + _teamListingFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _teamListingDataSource.ReadTotalTeams(filter).ConfigureAwait(false), CacheConstants.TeamsExpiration(), cacheKey, dependentCacheKey);
        }
    }
}
