using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Caching;
using Stoolball.Matches;
using Stoolball.Statistics;

namespace Stoolball.Data.Cache
{
    public class CachedPlayerPerformanceStatisticsDataSource : IPlayerPerformanceStatisticsDataSource
    {
        private readonly IReadThroughCache _readThroughCache;
        private readonly ICacheablePlayerPerformanceStatisticsDataSource _playerPerformanceStatisticsDataSource;
        private readonly IStatisticsFilterQueryStringSerializer _statisticsFilterSerializer;

        public CachedPlayerPerformanceStatisticsDataSource(IReadThroughCache readThroughCache, ICacheablePlayerPerformanceStatisticsDataSource playerPerformanceStatisticsDataSource, IStatisticsFilterQueryStringSerializer statisticsFilterSerializer)
        {
            _readThroughCache = readThroughCache ?? throw new System.ArgumentNullException(nameof(readThroughCache));
            _playerPerformanceStatisticsDataSource = playerPerformanceStatisticsDataSource ?? throw new System.ArgumentNullException(nameof(playerPerformanceStatisticsDataSource));
            _statisticsFilterSerializer = statisticsFilterSerializer ?? throw new System.ArgumentNullException(nameof(statisticsFilterSerializer));
        }

        public async Task<IEnumerable<StatisticsResult<PlayerInnings>>> ReadPlayerInnings(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cacheKey = nameof(ReadPlayerInnings) + _statisticsFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _playerPerformanceStatisticsDataSource.ReadPlayerInnings(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }
    }
}
