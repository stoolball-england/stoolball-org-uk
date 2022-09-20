using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Caching;
using Stoolball.Statistics;

namespace Stoolball.Data.Cache
{
    public class CachedBestPlayerTotalStatisticsDataSource : IBestPlayerTotalStatisticsDataSource
    {
        private readonly IReadThroughCache _readThroughCache;
        private readonly ICacheableBestPlayerTotalStatisticsDataSource _statisticsDataSource;
        private readonly IStatisticsFilterQueryStringSerializer _statisticsFilterSerializer;

        public CachedBestPlayerTotalStatisticsDataSource(IReadThroughCache readThroughCache, ICacheableBestPlayerTotalStatisticsDataSource statisticsDataSource, IStatisticsFilterQueryStringSerializer statisticsFilterSerializer)
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
            _statisticsDataSource = statisticsDataSource ?? throw new ArgumentNullException(nameof(statisticsDataSource));
            _statisticsFilterSerializer = statisticsFilterSerializer ?? throw new ArgumentNullException(nameof(statisticsFilterSerializer));
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostCatches(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadMostCatches) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadMostCatches(filter).ConfigureAwait(false), CacheConstants.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostRunOuts(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadMostRunOuts) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadMostRunOuts(filter).ConfigureAwait(false), CacheConstants.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostRunsScored(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadMostRunsScored) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadMostRunsScored(filter).ConfigureAwait(false), CacheConstants.StatisticsExpiration(), cacheKey, cacheKey);
        }

        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostWickets(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadMostWickets) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadMostWickets(filter).ConfigureAwait(false), CacheConstants.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalPlayersWithCatches(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadTotalPlayersWithCatches) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadTotalPlayersWithCatches(filter).ConfigureAwait(false), CacheConstants.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalPlayersWithRunOuts(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadTotalPlayersWithRunOuts) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadTotalPlayersWithRunOuts(filter).ConfigureAwait(false), CacheConstants.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalPlayersWithRunsScored(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadTotalPlayersWithRunsScored) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadTotalPlayersWithRunsScored(filter).ConfigureAwait(false), CacheConstants.StatisticsExpiration(), cacheKey, cacheKey);
        }

        public async Task<int> ReadTotalPlayersWithWickets(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadTotalPlayersWithWickets) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadTotalPlayersWithWickets(filter).ConfigureAwait(false), CacheConstants.StatisticsExpiration(), cacheKey, cacheKey);
        }
    }
}
