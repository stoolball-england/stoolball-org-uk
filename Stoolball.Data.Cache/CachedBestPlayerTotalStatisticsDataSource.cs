﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Registry;
using Stoolball.Statistics;

namespace Stoolball.Data.Cache
{
    public class CachedBestPlayerTotalStatisticsDataSource : IBestPlayerTotalStatisticsDataSource
    {
        private readonly ICacheableBestTotalStatisticsDataSource _statisticsDataSource;
        private readonly IReadOnlyPolicyRegistry<string> _policyRegistry;
        private readonly IStatisticsFilterSerializer _statisticsFilterSerializer;

        public CachedBestPlayerTotalStatisticsDataSource(IReadOnlyPolicyRegistry<string> policyRegistry, ICacheableBestTotalStatisticsDataSource statisticsDataSource, IStatisticsFilterSerializer statisticsFilterSerializer)
        {
            _policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
            _statisticsDataSource = statisticsDataSource ?? throw new ArgumentNullException(nameof(statisticsDataSource));
            _statisticsFilterSerializer = statisticsFilterSerializer ?? throw new ArgumentNullException(nameof(statisticsFilterSerializer));
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StatisticsResult<BestTotal>>> ReadMostRunsScored(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _statisticsDataSource.ReadMostRunsScored(filter).ConfigureAwait(false), new Context(nameof(ReadMostRunsScored) + _statisticsFilterSerializer.Serialize(filter)));
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalPlayersWithRunsScored(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _statisticsDataSource.ReadTotalPlayersWithRunsScored(filter).ConfigureAwait(false), new Context(nameof(ReadTotalPlayersWithRunsScored) + _statisticsFilterSerializer.Serialize(filter)));
        }
    }
}