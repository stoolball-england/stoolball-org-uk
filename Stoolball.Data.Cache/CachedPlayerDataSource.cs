using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Registry;
using Stoolball.Statistics;

namespace Stoolball.Data.Cache
{
    public class CachedPlayerDataSource : IPlayerDataSource
    {
        private readonly IReadOnlyPolicyRegistry<string> _policyRegistry;
        private readonly ICacheablePlayerDataSource _playerDataSource;

        public CachedPlayerDataSource(IReadOnlyPolicyRegistry<string> policyRegistry, ICacheablePlayerDataSource playerDataSource)
        {
            _policyRegistry = policyRegistry ?? throw new System.ArgumentNullException(nameof(policyRegistry));
            _playerDataSource = playerDataSource ?? throw new System.ArgumentNullException(nameof(playerDataSource));
        }

        public async Task<Player> ReadPlayerByRoute(string route)
        {
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.StatisticsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _playerDataSource.ReadPlayerByRoute(route).ConfigureAwait(false), new Context(nameof(ReadPlayerByRoute) + route));
        }

        public async Task<List<PlayerIdentity>> ReadPlayerIdentities(PlayerIdentityFilter filter)
        {
            return await _playerDataSource.ReadPlayerIdentities(filter).ConfigureAwait(false);
        }
    }
}
