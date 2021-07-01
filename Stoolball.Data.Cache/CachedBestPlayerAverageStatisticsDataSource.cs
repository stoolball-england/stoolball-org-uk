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
        private readonly IStatisticsFilterSerializer _statisticsFilterSerializer;

        public CachedBestPlayerAverageStatisticsDataSource(IReadOnlyPolicyRegistry<string> policyRegistry, ICacheableBestPlayerAverageStatisticsDataSource statisticsDataSource, IStatisticsFilterSerializer statisticsFilterSerializer)
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
        public async Task<int> ReadTotalPlayersWithBattingAverage(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _statisticsDataSource.ReadTotalPlayersWithBattingAverage(filter).ConfigureAwait(false), new Context(nameof(ReadTotalPlayersWithBattingAverage) + _statisticsFilterSerializer.Serialize(filter)));
        }
    }
}
