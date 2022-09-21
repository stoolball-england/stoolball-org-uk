using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Caching;
using Stoolball.Competitions;

namespace Stoolball.Data.Cache
{
    public class CachedCompetitionDataSource : ICompetitionDataSource
    {
        private readonly IReadThroughCache _readThroughCache;
        private readonly ICacheableCompetitionDataSource _competitionDataSource;
        private readonly ICompetitionFilterSerializer _competitionFilterSerializer;

        public CachedCompetitionDataSource(IReadThroughCache readThroughCache, ICacheableCompetitionDataSource competitionDataSource, ICompetitionFilterSerializer competitionFilterSerializer)
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _competitionFilterSerializer = competitionFilterSerializer ?? throw new ArgumentNullException(nameof(competitionFilterSerializer));
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalCompetitions(CompetitionFilter filter)
        {
            filter = filter ?? new CompetitionFilter();

            var cacheKey = nameof(ICompetitionDataSource) + nameof(ReadTotalCompetitions);
            var dependentCacheKey = cacheKey + _competitionFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _competitionDataSource.ReadTotalCompetitions(filter).ConfigureAwait(false), CachePolicy.CompetitionsExpiration(), cacheKey, dependentCacheKey);
        }

        /// <inheritdoc />
        public async Task<List<Competition>> ReadCompetitions(CompetitionFilter filter)
        {
            filter = filter ?? new CompetitionFilter();

            var cacheKey = nameof(ICompetitionDataSource) + nameof(ReadCompetitions);
            var dependentCacheKey = cacheKey + _competitionFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _competitionDataSource.ReadCompetitions(filter).ConfigureAwait(false), CachePolicy.CompetitionsExpiration(), cacheKey, dependentCacheKey);
        }

        /// <inheritdoc />
        public async Task<Competition> ReadCompetitionByRoute(string route)
        {
            return await _competitionDataSource.ReadCompetitionByRoute(route).ConfigureAwait(false);
        }
    }
}
