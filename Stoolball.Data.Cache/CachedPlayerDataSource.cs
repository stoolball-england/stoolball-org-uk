using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Caching;
using Stoolball.Statistics;

namespace Stoolball.Data.Cache
{
    public class CachedPlayerDataSource : IPlayerDataSource
    {
        private readonly IReadThroughCache _readThroughCache;
        private readonly ICacheablePlayerDataSource _playerDataSource;
        private readonly IPlayerFilterSerializer _playerFilterSerializer;
        private readonly IStatisticsFilterQueryStringSerializer _statisticsFilterSerialiser;

        public CachedPlayerDataSource(IReadThroughCache readThroughCache, ICacheablePlayerDataSource playerDataSource, IPlayerFilterSerializer playerFilterSerializer, IStatisticsFilterQueryStringSerializer statisticsFilterSerialiser)
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
            _playerFilterSerializer = playerFilterSerializer ?? throw new ArgumentNullException(nameof(playerFilterSerializer));
            _statisticsFilterSerialiser = statisticsFilterSerialiser ?? throw new ArgumentNullException(nameof(statisticsFilterSerialiser));
        }

        public async Task<Player> ReadPlayerByMemberKey(Guid key)
        {
            var cacheKey = nameof(IPlayerDataSource) + nameof(ReadPlayerByMemberKey) + key;
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _playerDataSource.ReadPlayerByMemberKey(key), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        public async Task<Player> ReadPlayerByRoute(string route, StatisticsFilter? filter = null)
        {
            var cacheKey = nameof(IPlayerDataSource) + nameof(ReadPlayerByRoute) + route;
            var dependentCacheKey = cacheKey + _statisticsFilterSerialiser.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _playerDataSource.ReadPlayerByRoute(route, filter), CachePolicy.StatisticsExpiration(), cacheKey, dependentCacheKey);
        }

        public async Task<List<PlayerIdentity>> ReadPlayerIdentities(PlayerFilter filter)
        {
            var cacheKey = nameof(IPlayerDataSource) + nameof(ReadPlayerIdentities) + GranularCacheKey(filter);
            var dependentCacheKey = cacheKey + _playerFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _playerDataSource.ReadPlayerIdentities(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, dependentCacheKey);
        }

        public async Task<List<Player>> ReadPlayers(PlayerFilter filter)
        {
            var cacheKey = nameof(IPlayerDataSource) + nameof(ReadPlayers) + _playerFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _playerDataSource.ReadPlayers(filter).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        public async Task<List<Player>> ReadPlayers(PlayerFilter filter, IDbConnection connection)
        {
            var cacheKey = nameof(IPlayerDataSource) + nameof(ReadPlayers) + _playerFilterSerializer.Serialize(filter);
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _playerDataSource.ReadPlayers(filter, connection).ConfigureAwait(false), CachePolicy.StatisticsExpiration(), cacheKey, cacheKey);
        }

        /// <summary>
        /// Maintain separate caches for common filtering cases so that we can invalidate the cache selectively
        /// </summary>
        /// <param name="filter"></param>
        /// <returns>If the filter includes exactly one of the common cases, an additional key. Otherwise <c>null</c>.</returns>
        private string? GranularCacheKey(PlayerFilter filter)
        {
            string? granularKey = null;

            // Recognise the common case of a team (or teams, for awards) and a query, used for player autocomplete lookups
            if (filter.TeamIds.Any() && !filter.ClubIds.Any() && !filter.CompetitionIds.Any() && !filter.MatchLocationIds.Any() && !filter.PlayerIdentityIds.Any() && !filter.PlayerIds.Any() && !filter.SeasonIds.Any())
            {
                granularKey = "ForTeams" + string.Join("--", filter.TeamIds.OrderBy(x => x.ToString()));
            }

            return granularKey;
        }
    }
}
