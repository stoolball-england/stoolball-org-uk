using System;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;

namespace Stoolball.Data.MemoryCache
{
    public class CompetitionListingCacheInvalidator : IListingCacheInvalidator<Competition>
    {
        private readonly IReadThroughCache _readThroughCache;

        public CompetitionListingCacheInvalidator(IReadThroughCache readThroughCache)
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
        }

        public void InvalidateCache()
        {
            _readThroughCache.InvalidateCache(nameof(ICompetitionDataSource) + nameof(ICompetitionDataSource.ReadTotalCompetitions));
            _readThroughCache.InvalidateCache(nameof(ICompetitionDataSource) + nameof(ICompetitionDataSource.ReadCompetitions));
        }
    }
}
