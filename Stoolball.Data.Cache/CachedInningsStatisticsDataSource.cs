using System.Threading.Tasks;
using Polly;
using Polly.Registry;
using Stoolball.Caching;
using Stoolball.Statistics;

namespace Stoolball.Data.Cache
{
    public class CachedInningsStatisticsDataSource : IInningsStatisticsDataSource
    {
        private readonly IReadOnlyPolicyRegistry<string> _policyRegistry;
        private readonly ICacheableInningsStatisticsDataSource _inningsStatisticsDataSource;
        private readonly IStatisticsFilterSerializer _statisticsFilterSerializer;

        public CachedInningsStatisticsDataSource(IReadOnlyPolicyRegistry<string> policyRegistry, ICacheableInningsStatisticsDataSource inningsStatisticsDataSource, IStatisticsFilterSerializer statisticsFilterSerializer)
        {
            _policyRegistry = policyRegistry ?? throw new System.ArgumentNullException(nameof(policyRegistry));
            _inningsStatisticsDataSource = inningsStatisticsDataSource ?? throw new System.ArgumentNullException(nameof(inningsStatisticsDataSource));
            _statisticsFilterSerializer = statisticsFilterSerializer ?? throw new System.ArgumentNullException(nameof(statisticsFilterSerializer));
        }

        public async Task<InningsStatistics> ReadInningsStatistics(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _inningsStatisticsDataSource.ReadInningsStatistics(filter).ConfigureAwait(false), new Context(nameof(ReadInningsStatistics) + _statisticsFilterSerializer.Serialize(filter)));
        }
    }
}
