using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Data.Abstractions;
using Stoolball.Statistics;

namespace Stoolball.Data.MemoryCache
{
    public class CachedBestPlayerAverageStatisticsDataSource : IBestPlayerAverageStatisticsDataSource
    {
        private readonly IReadThroughCache _readThroughCache;
        private readonly ICacheableBestPlayerAverageStatisticsDataSource _statisticsDataSource;
        private readonly IStatisticsFilterQueryStringSerializer _statisticsFilterSerializer;

        public CachedBestPlayerAverageStatisticsDataSource(IReadThroughCache readThroughCache, ICacheableBestPlayerAverageStatisticsDataSource statisticsDataSource, IStatisticsFilterQueryStringSerializer statisticsFilterSerializer)
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
            _statisticsDataSource = statisticsDataSource ?? throw new ArgumentNullException(nameof(statisticsDataSource));
            _statisticsFilterSerializer = statisticsFilterSerializer ?? throw new ArgumentNullException(nameof(statisticsFilterSerializer));
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestBattingAverage(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadBestBattingAverage) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadBestBattingAverage(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestBattingStrikeRate(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadBestBattingStrikeRate) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadBestBattingStrikeRate(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestBowlingAverage(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadBestBowlingAverage) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadBestBowlingAverage(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestBowlingStrikeRate(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadBestBowlingStrikeRate) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadBestBowlingStrikeRate(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);

        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestEconomyRate(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadBestEconomyRate) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadBestEconomyRate(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalPlayersWithBattingAverage(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadTotalPlayersWithBattingAverage) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadTotalPlayersWithBattingAverage(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalPlayersWithBattingStrikeRate(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadTotalPlayersWithBattingStrikeRate) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadTotalPlayersWithBattingStrikeRate(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalPlayersWithBowlingAverage(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadTotalPlayersWithBowlingAverage) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadTotalPlayersWithBowlingAverage(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalPlayersWithBowlingStrikeRate(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadTotalPlayersWithBowlingStrikeRate) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadTotalPlayersWithBowlingStrikeRate(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalPlayersWithEconomyRate(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadTotalPlayersWithEconomyRate) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _statisticsDataSource.ReadTotalPlayersWithEconomyRate(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }
    }
}
