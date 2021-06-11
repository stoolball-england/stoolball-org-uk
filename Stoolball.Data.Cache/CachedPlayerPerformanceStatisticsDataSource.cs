using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Registry;
using Stoolball.Caching;
using Stoolball.Matches;
using Stoolball.Statistics;

namespace Stoolball.Data.Cache
{
    public class CachedPlayerPerformanceStatisticsDataSource : IPlayerPerformanceStatisticsDataSource
    {
        private readonly IReadOnlyPolicyRegistry<string> _policyRegistry;
        private readonly ICacheablePlayerPerformanceStatisticsDataSource _playerPerformanceStatisticsDataSource;
        private readonly IStatisticsFilterSerializer _statisticsFilterSerializer;

        public CachedPlayerPerformanceStatisticsDataSource(IReadOnlyPolicyRegistry<string> policyRegistry, ICacheablePlayerPerformanceStatisticsDataSource playerPerformanceStatisticsDataSource, IStatisticsFilterSerializer statisticsFilterSerializer)
        {
            _policyRegistry = policyRegistry ?? throw new System.ArgumentNullException(nameof(policyRegistry));
            _playerPerformanceStatisticsDataSource = playerPerformanceStatisticsDataSource ?? throw new System.ArgumentNullException(nameof(playerPerformanceStatisticsDataSource));
            _statisticsFilterSerializer = statisticsFilterSerializer ?? throw new System.ArgumentNullException(nameof(statisticsFilterSerializer));
        }

        public async Task<IEnumerable<StatisticsResult<PlayerInnings>>> ReadPlayerInnings(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _playerPerformanceStatisticsDataSource.ReadPlayerInnings(filter).ConfigureAwait(false), new Context(nameof(ReadPlayerInnings) + _statisticsFilterSerializer.Serialize(filter)));
        }
    }
}
