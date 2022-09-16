using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Polly;
using Polly.Registry;
using Stoolball.Caching;
using Stoolball.Statistics;

namespace Stoolball.Data.Cache
{
    public class CachedPlayerDataSource : IPlayerDataSource
    {
        private readonly IReadOnlyPolicyRegistry<string> _policyRegistry;
        private readonly IReadThroughCache _readThroughCache;
        private readonly ICacheablePlayerDataSource _playerDataSource;
        private readonly IPlayerFilterSerializer _playerFilterSerializer;
        private readonly IStatisticsFilterQueryStringSerializer _statisticsFilterSerialiser;

        public CachedPlayerDataSource(IReadOnlyPolicyRegistry<string> policyRegistry, IReadThroughCache readThroughCache, ICacheablePlayerDataSource playerDataSource, IPlayerFilterSerializer playerFilterSerializer, IStatisticsFilterQueryStringSerializer statisticsFilterSerialiser)
        {
            _policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
            _playerFilterSerializer = playerFilterSerializer ?? throw new ArgumentNullException(nameof(playerFilterSerializer));
            _statisticsFilterSerialiser = statisticsFilterSerialiser ?? throw new ArgumentNullException(nameof(statisticsFilterSerialiser));
        }

        public async Task<Player> ReadPlayerByMemberKey(Guid key)
        {
            var cacheKey = nameof(ReadPlayerByMemberKey) + key;
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _playerDataSource.ReadPlayerByMemberKey(key), CacheConstants.StatisticsExpiration(), cacheKey, cacheKey);
        }

        public async Task<Player> ReadPlayerByRoute(string route, StatisticsFilter? filter = null)
        {
            var cacheKey = nameof(ReadPlayerByRoute) + route;
            var dependentCacheKey = cacheKey + _statisticsFilterSerialiser.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _playerDataSource.ReadPlayerByRoute(route, filter), CacheConstants.StatisticsExpiration(), cacheKey, dependentCacheKey);
        }

        public async Task<List<PlayerIdentity>> ReadPlayerIdentities(PlayerFilter filter)
        {
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _playerDataSource.ReadPlayerIdentities(filter).ConfigureAwait(false), new Context(nameof(ReadPlayerIdentities) + _playerFilterSerializer.Serialize(filter)));
        }

        public async Task<List<Player>> ReadPlayers(PlayerFilter filter)
        {
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _playerDataSource.ReadPlayers(filter).ConfigureAwait(false), new Context(nameof(ReadPlayers) + _playerFilterSerializer.Serialize(filter)));
        }

        public async Task<List<Player>> ReadPlayers(PlayerFilter filter, IDbConnection connection)
        {
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _playerDataSource.ReadPlayers(filter, connection).ConfigureAwait(false), new Context(nameof(ReadPlayers) + _playerFilterSerializer.Serialize(filter)));
        }
    }
}
