using System;
using System.Collections.Generic;
using System.Linq;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
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

        /// <inheritdoc/>
        public void InvalidateCacheForPlayer(Player player)
        {
            if (player is null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (string.IsNullOrEmpty(player.PlayerRoute))
            {
                throw new ArgumentException($"{nameof(player.PlayerRoute)} cannot be null or empty string");
            }

            var normalisedRoute = _routeNormaliser.NormaliseRouteToEntity(player.PlayerRoute, "players");
            _readThroughCache.InvalidateCache(nameof(IPlayerDataSource) + nameof(IPlayerDataSource.ReadPlayerByRoute) + normalisedRoute);
            if (player.MemberKey.HasValue)
            {
                _readThroughCache.InvalidateCache(nameof(IPlayerDataSource) + nameof(IPlayerDataSource.ReadPlayerByMemberKey) + player.MemberKey);
            }
            _readThroughCache.InvalidateCache(nameof(IPlayerSummaryStatisticsDataSource) + nameof(IPlayerSummaryStatisticsDataSource.ReadBattingStatistics) + normalisedRoute);
            _readThroughCache.InvalidateCache(nameof(IPlayerSummaryStatisticsDataSource) + nameof(IPlayerSummaryStatisticsDataSource.ReadBowlingStatistics) + normalisedRoute);
            _readThroughCache.InvalidateCache(nameof(IPlayerSummaryStatisticsDataSource) + nameof(IPlayerSummaryStatisticsDataSource.ReadFieldingStatistics) + normalisedRoute);
        }

        /// <inheritdoc/>
        public void InvalidateCacheForTeams(IEnumerable<TeamInMatch> teamsInMatch)
        {
            if (teamsInMatch is null) { throw new ArgumentNullException(nameof(teamsInMatch)); }

            var teamIds = string.Join("--", teamsInMatch.Select(tim => tim.Team?.TeamId).OfType<Guid>().Distinct().OrderBy(x => x.ToString()));
            _readThroughCache.InvalidateCache(nameof(IPlayerDataSource) + nameof(IPlayerDataSource.ReadPlayerIdentities) + "ForTeams" + teamIds);
        }

        /// <inheritdoc/>
        public void InvalidateCacheForTeams(params Team[] teams)
        {
            if (teams == null) { throw new ArgumentNullException(nameof(teams)); }

            foreach (var teamId in teams.Select(t => t.TeamId).OfType<Guid>().Distinct())
            {
                _readThroughCache.InvalidateCache(nameof(IPlayerDataSource) + nameof(IPlayerDataSource.ReadPlayerIdentities) + "ForTeams" + teamId);
            }
        }
    }
}
