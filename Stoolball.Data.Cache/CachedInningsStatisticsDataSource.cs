using System.Threading.Tasks;
using Stoolball.Caching;
using Stoolball.Statistics;

namespace Stoolball.Data.Cache
{
    public class CachedInningsStatisticsDataSource : IInningsStatisticsDataSource
    {
        private readonly IReadThroughCache _readThroughCache;
        private readonly ICacheableInningsStatisticsDataSource _inningsStatisticsDataSource;
        private readonly IStatisticsFilterQueryStringSerializer _statisticsFilterSerializer;

        public CachedInningsStatisticsDataSource(IReadThroughCache readThroughCache, ICacheableInningsStatisticsDataSource inningsStatisticsDataSource, IStatisticsFilterQueryStringSerializer statisticsFilterSerializer)
        {
            _readThroughCache = readThroughCache ?? throw new System.ArgumentNullException(nameof(readThroughCache));
            _inningsStatisticsDataSource = inningsStatisticsDataSource ?? throw new System.ArgumentNullException(nameof(inningsStatisticsDataSource));
            _statisticsFilterSerializer = statisticsFilterSerializer ?? throw new System.ArgumentNullException(nameof(statisticsFilterSerializer));
        }

        public async Task<InningsStatistics> ReadInningsStatistics(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadInningsStatistics) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _inningsStatisticsDataSource.ReadInningsStatistics(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }
    }
}
