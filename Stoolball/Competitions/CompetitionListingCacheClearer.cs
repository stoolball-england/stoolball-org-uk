using System;
using Stoolball.Caching;

namespace Stoolball.Competitions
{
    public class CompetitionListingCacheClearer : IListingCacheClearer<Competition>
    {
        private readonly IReadThroughCache _readThroughCache;

        public CompetitionListingCacheClearer(IReadThroughCache readThroughCache)
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
        }

        public void ClearCache()
        {
            _readThroughCache.InvalidateCache(nameof(ICompetitionDataSource) + nameof(ICompetitionDataSource.ReadTotalCompetitions));
            _readThroughCache.InvalidateCache(nameof(ICompetitionDataSource) + nameof(ICompetitionDataSource.ReadCompetitions));
        }
    }
}
