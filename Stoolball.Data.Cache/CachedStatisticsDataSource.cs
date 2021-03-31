using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Registry;
using Stoolball.Data.SqlServer;
using Stoolball.Matches;
using Stoolball.Statistics;

namespace Stoolball.Data.Cache
{
    public class CachedStatisticsDataSource : SqlServerStatisticsDataSource
    {
        private readonly IReadOnlyPolicyRegistry<string> _policyRegistry;
        private readonly IStatisticsFilterSerializer _statisticsFilterSerializer;

        public CachedStatisticsDataSource(IDatabaseConnectionFactory databaseConnectionFactory, IReadOnlyPolicyRegistry<string> policyRegistry, IStatisticsFilterSerializer statisticsFilterSerializer) : base(databaseConnectionFactory)
        {
            _policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
            _statisticsFilterSerializer = statisticsFilterSerializer ?? throw new ArgumentNullException(nameof(statisticsFilterSerializer));
        }

        public async override Task<IEnumerable<StatisticsResult<PlayerInnings>>> ReadPlayerInnings(StatisticsFilter filter, StatisticsSortOrder sortOrder)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await base.ReadPlayerInnings(filter, sortOrder), new Context(nameof(ReadPlayerInnings) + _statisticsFilterSerializer.Serialize(filter) + sortOrder.ToString()));
        }

        public async override Task<IEnumerable<StatisticsResult<BowlingFigures>>> ReadBowlingFigures(StatisticsFilter filter, StatisticsSortOrder sortOrder)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await base.ReadBowlingFigures(filter, sortOrder), new Context(nameof(ReadBowlingFigures) + _statisticsFilterSerializer.Serialize(filter) + sortOrder.ToString()));
        }
    }
}
