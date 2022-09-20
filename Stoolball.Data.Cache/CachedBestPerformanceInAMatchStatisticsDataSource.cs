using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Caching;
using Stoolball.Matches;
using Stoolball.Statistics;

namespace Stoolball.Data.Cache
{
    public class CachedBestPerformanceInAMatchStatisticsDataSource : IBestPerformanceInAMatchStatisticsDataSource
    {
        private readonly IReadThroughCache _readThroughCache;
        private readonly ICacheableBestPerformanceInAMatchStatisticsDataSource _statisticsDataSource;
        private readonly IStatisticsFilterQueryStringSerializer _statisticsFilterSerializer;

        public CachedBestPerformanceInAMatchStatisticsDataSource(IReadThroughCache readThroughCache, ICacheableBestPerformanceInAMatchStatisticsDataSource statisticsDataSource, IStatisticsFilterQueryStringSerializer statisticsFilterSerializer)
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
            _statisticsDataSource = statisticsDataSource ?? throw new ArgumentNullException(nameof(statisticsDataSource));
            _statisticsFilterSerializer = statisticsFilterSerializer ?? throw new ArgumentNullException(nameof(statisticsFilterSerializer));
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<PlayerInnings>>> ReadPlayerInnings(StatisticsFilter filter, StatisticsSortOrder sortOrder)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadPlayerInnings) + _statisticsFilterSerializer.Serialize(filter) + sortOrder.ToString();
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadPlayerInnings(filter, sortOrder).ConfigureAwait(false), CacheConstants.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BowlingFigures>>> ReadBowlingFigures(StatisticsFilter filter, StatisticsSortOrder sortOrder)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadBowlingFigures) + _statisticsFilterSerializer.Serialize(filter) + sortOrder.ToString();
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadBowlingFigures(filter, sortOrder).ConfigureAwait(false), CacheConstants.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalPlayerInnings(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadTotalPlayerInnings) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadTotalPlayerInnings(filter).ConfigureAwait(false), CacheConstants.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalBowlingFigures(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadTotalBowlingFigures) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadTotalBowlingFigures(filter).ConfigureAwait(false), CacheConstants.StatisticsExpiration(), cacheKey, cacheKey);
        }
    }
}
