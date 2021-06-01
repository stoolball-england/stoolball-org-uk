using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Registry;
using Stoolball.Matches;
using Stoolball.Statistics;

namespace Stoolball.Data.Cache
{
    public class CachedBestPerformanceInAMatchStatisticsDataSource : IBestPerformanceInAMatchStatisticsDataSource
    {
        private readonly ICacheableBestPerformanceInAMatchStatisticsDataSource _statisticsDataSource;
        private readonly IReadOnlyPolicyRegistry<string> _policyRegistry;
        private readonly IStatisticsFilterSerializer _statisticsFilterSerializer;

        public CachedBestPerformanceInAMatchStatisticsDataSource(IReadOnlyPolicyRegistry<string> policyRegistry, ICacheableBestPerformanceInAMatchStatisticsDataSource statisticsDataSource, IStatisticsFilterSerializer statisticsFilterSerializer)
        {
            _policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
            _statisticsDataSource = statisticsDataSource ?? throw new ArgumentNullException(nameof(statisticsDataSource));
            _statisticsFilterSerializer = statisticsFilterSerializer ?? throw new ArgumentNullException(nameof(statisticsFilterSerializer));
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<PlayerInnings>>> ReadPlayerInnings(StatisticsFilter filter, StatisticsSortOrder sortOrder)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _statisticsDataSource.ReadPlayerInnings(filter, sortOrder).ConfigureAwait(false), new Context(nameof(ReadPlayerInnings) + _statisticsFilterSerializer.Serialize(filter) + sortOrder.ToString()));
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BowlingFigures>>> ReadBowlingFigures(StatisticsFilter filter, StatisticsSortOrder sortOrder)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _statisticsDataSource.ReadBowlingFigures(filter, sortOrder).ConfigureAwait(false), new Context(nameof(ReadBowlingFigures) + _statisticsFilterSerializer.Serialize(filter) + sortOrder.ToString()));
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalPlayerInnings(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _statisticsDataSource.ReadTotalPlayerInnings(filter).ConfigureAwait(false), new Context(nameof(ReadTotalPlayerInnings) + _statisticsFilterSerializer.Serialize(filter)));
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalBowlingFigures(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _statisticsDataSource.ReadTotalBowlingFigures(filter).ConfigureAwait(false), new Context(nameof(ReadTotalBowlingFigures) + _statisticsFilterSerializer.Serialize(filter)));
        }
    }
}
