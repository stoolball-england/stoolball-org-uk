using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Data.Abstractions;
using Stoolball.Statistics;

namespace Stoolball.Data.MemoryCache
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
            var cacheKey = nameof(IBestPlayerTotalStatisticsDataSource) + nameof(ReadMostCatches) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadMostCatches(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostRunOuts(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(IBestPlayerTotalStatisticsDataSource) + nameof(ReadMostRunOuts) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadMostRunOuts(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostPlayerInnings(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(IBestPlayerTotalStatisticsDataSource) + nameof(ReadMostPlayerInnings) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadMostPlayerInnings(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostRunsScored(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(IBestPlayerTotalStatisticsDataSource) + nameof(ReadMostRunsScored) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadMostRunsScored(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostWickets(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(IBestPlayerTotalStatisticsDataSource) + nameof(ReadMostWickets) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadMostWickets(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostInningsWithBowling(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(IBestPlayerTotalStatisticsDataSource) + nameof(ReadMostInningsWithBowling) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadMostInningsWithBowling(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadMostPlayerOfTheMatchAwards(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(IBestPlayerTotalStatisticsDataSource) + nameof(ReadMostPlayerOfTheMatchAwards) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadMostPlayerOfTheMatchAwards(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalPlayersWithCatches(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(IBestPlayerTotalStatisticsDataSource) + nameof(ReadTotalPlayersWithCatches) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadTotalPlayersWithCatches(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalPlayersWithPlayerOfTheMatchAwards(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(IBestPlayerTotalStatisticsDataSource) + nameof(ReadTotalPlayersWithPlayerOfTheMatchAwards) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadTotalPlayersWithPlayerOfTheMatchAwards(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalPlayersWithRunOuts(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(IBestPlayerTotalStatisticsDataSource) + nameof(ReadTotalPlayersWithRunOuts) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadTotalPlayersWithRunOuts(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalPlayersWithRunsScored(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(IBestPlayerTotalStatisticsDataSource) + nameof(ReadTotalPlayersWithRunsScored) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadTotalPlayersWithRunsScored(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalPlayersWithWickets(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(IBestPlayerTotalStatisticsDataSource) + nameof(ReadTotalPlayersWithWickets) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadTotalPlayersWithWickets(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }
    }
}
