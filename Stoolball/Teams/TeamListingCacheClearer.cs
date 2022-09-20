using System;
using Stoolball.Caching;

namespace Stoolball.Teams
{
    public class TeamListingCacheClearer : IListingCacheClearer<Team>
    {
        private readonly IReadThroughCache _readThroughCache;

        public TeamListingCacheClearer(IReadThroughCache readThroughCache)
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
        }

        public void ClearCache()
        {
            _readThroughCache.InvalidateCache(nameof(ITeamListingDataSource) + nameof(ITeamListingDataSource.ReadTeamListings));
            _readThroughCache.InvalidateCache(nameof(ITeamListingDataSource) + nameof(ITeamListingDataSource.ReadTotalTeams));
        }
    }
}
