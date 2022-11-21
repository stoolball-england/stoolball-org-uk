using System;
using Stoolball.Data.Abstractions;
using Stoolball.Teams;

namespace Stoolball.Data.MemoryCache
{
    public class TeamListingCacheInvalidator : IListingCacheInvalidator<Team>
    {
        private readonly IReadThroughCache _readThroughCache;

        public TeamListingCacheInvalidator(IReadThroughCache readThroughCache)
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
        }

        public void InvalidateCache()
        {
            _readThroughCache.InvalidateCache(nameof(ITeamListingDataSource) + nameof(ITeamListingDataSource.ReadTeamListings));
            _readThroughCache.InvalidateCache(nameof(ITeamListingDataSource) + nameof(ITeamListingDataSource.ReadTotalTeams));
        }
    }
}
