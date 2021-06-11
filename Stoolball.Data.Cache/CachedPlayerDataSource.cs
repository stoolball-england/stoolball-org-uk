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
        private readonly ICacheablePlayerDataSource _playerDataSource;
        private readonly IPlayerFilterSerializer _playerFilterSerializer;

        public CachedPlayerDataSource(IReadOnlyPolicyRegistry<string> policyRegistry, ICacheablePlayerDataSource playerDataSource, IPlayerFilterSerializer playerFilterSerializer)
        {
            _policyRegistry = policyRegistry ?? throw new System.ArgumentNullException(nameof(policyRegistry));
            _playerDataSource = playerDataSource ?? throw new System.ArgumentNullException(nameof(playerDataSource));
            _playerFilterSerializer = playerFilterSerializer ?? throw new System.ArgumentNullException(nameof(playerFilterSerializer));
        }

        public async Task<Player> ReadPlayerByRoute(string route)
        {
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _playerDataSource.ReadPlayerByRoute(route).ConfigureAwait(false), new Context(nameof(ReadPlayerByRoute) + route));
        }

        public async Task<List<PlayerIdentity>> ReadPlayerIdentities(PlayerFilter filter)
        {
            return await _playerDataSource.ReadPlayerIdentities(filter).ConfigureAwait(false);
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
