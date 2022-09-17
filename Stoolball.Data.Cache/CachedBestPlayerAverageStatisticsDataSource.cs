using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Registry;
using Stoolball.Caching;
using Stoolball.Statistics;

namespace Stoolball.Data.Cache
{
    public class CachedBestPlayerAverageStatisticsDataSource : IBestPlayerAverageStatisticsDataSource
    {
        private readonly ICacheableBestPlayerAverageStatisticsDataSource _statisticsDataSource;
        private readonly IReadOnlyPolicyRegistry<string> _policyRegistry;
        private readonly IStatisticsFilterQueryStringSerializer _statisticsFilterSerializer;

        public CachedBestPlayerAverageStatisticsDataSource(IReadOnlyPolicyRegistry<string> policyRegistry, ICacheableBestPlayerAverageStatisticsDataSource statisticsDataSource, IStatisticsFilterQueryStringSerializer statisticsFilterSerializer)
        {
            _policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
            _statisticsDataSource = statisticsDataSource ?? throw new ArgumentNullException(nameof(statisticsDataSource));
            _statisticsFilterSerializer = statisticsFilterSerializer ?? throw new ArgumentNullException(nameof(statisticsFilterSerializer));
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestBattingAverage(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _statisticsDataSource.ReadBestBattingAverage(filter).ConfigureAwait(false), new Context(nameof(ReadBestBattingAverage) + _statisticsFilterSerializer.Serialize(filter)));
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestBattingStrikeRate(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _statisticsDataSource.ReadBestBattingStrikeRate(filter).ConfigureAwait(false), new Context(nameof(ReadBestBattingStrikeRate) + _statisticsFilterSerializer.Serialize(filter)));
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestBowlingAverage(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _statisticsDataSource.ReadBestBowlingAverage(filter).ConfigureAwait(false), new Context(nameof(ReadBestBowlingAverage) + _statisticsFilterSerializer.Serialize(filter)));
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestBowlingStrikeRate(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _statisticsDataSource.ReadBestBowlingStrikeRate(filter).ConfigureAwait(false), new Context(nameof(ReadBestBowlingStrikeRate) + _statisticsFilterSerializer.Serialize(filter)));

        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BestStatistic>>> ReadBestEconomyRate(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _statisticsDataSource.ReadBestEconomyRate(filter).ConfigureAwait(false), new Context(nameof(ReadBestEconomyRate) + _statisticsFilterSerializer.Serialize(filter)));
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalPlayersWithBattingAverage(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _statisticsDataSource.ReadTotalPlayersWithBattingAverage(filter).ConfigureAwait(false), new Context(nameof(ReadTotalPlayersWithBattingAverage) + _statisticsFilterSerializer.Serialize(filter)));
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalPlayersWithBattingStrikeRate(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _statisticsDataSource.ReadTotalPlayersWithBattingStrikeRate(filter).ConfigureAwait(false), new Context(nameof(ReadTotalPlayersWithBattingStrikeRate) + _statisticsFilterSerializer.Serialize(filter)));
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalPlayersWithBowlingAverage(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _statisticsDataSource.ReadTotalPlayersWithBowlingAverage(filter).ConfigureAwait(false), new Context(nameof(ReadTotalPlayersWithBowlingAverage) + _statisticsFilterSerializer.Serialize(filter)));
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalPlayersWithBowlingStrikeRate(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _statisticsDataSource.ReadTotalPlayersWithBowlingStrikeRate(filter).ConfigureAwait(false), new Context(nameof(ReadTotalPlayersWithBowlingStrikeRate) + _statisticsFilterSerializer.Serialize(filter)));
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalPlayersWithEconomyRate(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _statisticsDataSource.ReadTotalPlayersWithEconomyRate(filter).ConfigureAwait(false), new Context(nameof(ReadTotalPlayersWithEconomyRate) + _statisticsFilterSerializer.Serialize(filter)));
        }
    }
}
