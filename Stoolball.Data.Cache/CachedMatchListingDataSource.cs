using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Caching;
using Stoolball.Matches;

namespace Stoolball.Data.Cache
{
    public class CachedMatchListingDataSource : IMatchListingDataSource
    {
        private readonly IReadThroughCache _readThroughCache;
        private readonly ICacheableMatchListingDataSource _matchListingDataSource;
        private readonly IMatchFilterQueryStringSerializer _matchFilterSerializer;

        public CachedMatchListingDataSource(IReadThroughCache readThroughCache, ICacheableMatchListingDataSource matchListingDataSource, IMatchFilterQueryStringSerializer matchFilterSerializer)
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
            _matchListingDataSource = matchListingDataSource ?? throw new ArgumentNullException(nameof(matchListingDataSource));
            _matchFilterSerializer = matchFilterSerializer ?? throw new ArgumentNullException(nameof(matchFilterSerializer));
        }

        /// <inheritdoc />
        public async Task<List<MatchListing>> ReadMatchListings(MatchFilter filter, MatchSortOrder sortOrder)
        {
            filter = filter ?? new MatchFilter();
            var cacheKey = nameof(IMatchListingDataSource) + nameof(ReadMatchListings) + GranularCacheKey(filter);
            var dependentCacheKey = cacheKey + _matchFilterSerializer.Serialize(filter) + sortOrder.ToString();
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _matchListingDataSource.ReadMatchListings(filter, sortOrder), CachePolicy.MatchesExpiration(), cacheKey, dependentCacheKey);
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalMatches(MatchFilter filter)
        {
            filter = filter ?? new MatchFilter();
            var cacheKey = nameof(IMatchListingDataSource) + nameof(ReadTotalMatches) + GranularCacheKey(filter);
            var dependentCacheKey = cacheKey + _matchFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _matchListingDataSource.ReadTotalMatches(filter).ConfigureAwait(false), CachePolicy.MatchesExpiration(), cacheKey, dependentCacheKey);
        }

        /// <summary>
        /// Maintain separate caches for common filtering cases so that, for example, updating a match affecting 2 teams does not clear listings for other teams
        /// </summary>
        /// <param name="filter"></param>
        /// <returns>If the filter includes exactly one of the common cases, an additional key. Otherwise <c>null</c>.</returns>
        private string? GranularCacheKey(MatchFilter filter)
        {
            string? granularKey = null;

            if (filter.TeamIds.Count == 1)
            {
                granularKey = "ForTeam" + filter.TeamIds[0];
            }
            else if (filter.TeamIds.Count > 1)
            {
                granularKey = "ForTeams" + string.Join("--", filter.TeamIds.OrderBy(x => x.ToString()));
            }
            //if (filter.MatchLocationIds.Any())
            //{
            //    if (string.IsNullOrEmpty(granularKey))
            //    {
            //        granularKey = "matchlocation";
            //    }
            //    else
            //    {
            //        return null;
            //    }
            //}
            //if (filter.SeasonIds.Any())
            //{
            //    if (string.IsNullOrEmpty(granularKey))
            //    {
            //        granularKey = "season";
            //    }
            //    else
            //    {
            //        return null;
            //    }
            //}

            return granularKey;
        }
    }
}
