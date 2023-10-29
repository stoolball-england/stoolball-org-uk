using System;
using System.Linq;
using Stoolball.Data.Abstractions;
using Stoolball.Routing;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball.Data.MemoryCache
{
    public class PlayerCacheInvalidator : IPlayerCacheInvalidator
    {
        private readonly IReadThroughCache _readThroughCache;
        private readonly IRouteNormaliser _routeNormaliser;

        public PlayerCacheInvalidator(IReadThroughCache readThroughCache, IRouteNormaliser routeNormaliser)
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
            _routeNormaliser = routeNormaliser ?? throw new ArgumentNullException(nameof(routeNormaliser));
        }

        public void InvalidateCacheForPlayer(Player cacheable)
        {
            if (cacheable is null)
            {
                throw new ArgumentNullException(nameof(cacheable));
            }

            if (string.IsNullOrEmpty(cacheable.PlayerRoute))
            {
                throw new ArgumentException($"{nameof(cacheable.PlayerRoute)} cannot be null or empty string");
            }

            var normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(cacheable.PlayerRoute, "players");
            _readThroughCache.InvalidateCache(nameof(IPlayerDataSource) + nameof(IPlayerDataSource.ReadPlayerByRoute) + normalisedRoute);
            if (cacheable.MemberKey.HasValue)
            {
                _readThroughCache.InvalidateCache(nameof(IPlayerDataSource) + nameof(IPlayerDataSource.ReadPlayerByMemberKey) + cacheable.MemberKey);
            }
            _readThroughCache.InvalidateCache(nameof(IPlayerSummaryStatisticsDataSource) + nameof(IPlayerSummaryStatisticsDataSource.ReadBattingStatistics) + normalisedRoute);
            _readThroughCache.InvalidateCache(nameof(IPlayerSummaryStatisticsDataSource) + nameof(IPlayerSummaryStatisticsDataSource.ReadBowlingStatistics) + normalisedRoute);
            _readThroughCache.InvalidateCache(nameof(IPlayerSummaryStatisticsDataSource) + nameof(IPlayerSummaryStatisticsDataSource.ReadFieldingStatistics) + normalisedRoute);
        }

        public void InvalidateCacheForTeams(params Team[] teams)
        {
            if (teams == null) { throw new ArgumentNullException(nameof(teams)); }

            var teamIds = string.Join("--", teams.Where(x => x?.TeamId != null).Select(x => x.TeamId!.Value).OrderBy(x => x.ToString()));
            _readThroughCache.InvalidateCache(nameof(IPlayerDataSource) + nameof(IPlayerDataSource.ReadPlayerIdentities) + "ForTeams" + teamIds);
        }
    }
}
