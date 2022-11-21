using System;
using System.Linq;
using Stoolball.Data.Abstractions;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball.Data.MemoryCache
{
    public class PlayerCacheInvalidator : IPlayerCacheInvalidator
    {
        private readonly IReadThroughCache _readThroughCache;

        public PlayerCacheInvalidator(IReadThroughCache readThroughCache)
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
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

            _readThroughCache.InvalidateCache(nameof(IPlayerDataSource) + nameof(IPlayerDataSource.ReadPlayerByRoute) + cacheable.PlayerRoute);
            if (cacheable.MemberKey.HasValue)
            {
                _readThroughCache.InvalidateCache(nameof(IPlayerDataSource) + nameof(IPlayerDataSource.ReadPlayerByMemberKey) + cacheable.MemberKey);
            }
            _readThroughCache.InvalidateCache(nameof(IPlayerSummaryStatisticsDataSource) + nameof(IPlayerSummaryStatisticsDataSource.ReadBattingStatistics) + cacheable.PlayerRoute);
            _readThroughCache.InvalidateCache(nameof(IPlayerSummaryStatisticsDataSource) + nameof(IPlayerSummaryStatisticsDataSource.ReadBowlingStatistics) + cacheable.PlayerRoute);
            _readThroughCache.InvalidateCache(nameof(IPlayerSummaryStatisticsDataSource) + nameof(IPlayerSummaryStatisticsDataSource.ReadFieldingStatistics) + cacheable.PlayerRoute);
        }

        public void InvalidateCacheForTeams(params Team[] teams)
        {
            if (teams == null) { throw new ArgumentNullException(nameof(teams)); }

            var teamIds = string.Join("--", teams.Where(x => x?.TeamId != null).Select(x => x.TeamId!.Value).OrderBy(x => x.ToString()));
            _readThroughCache.InvalidateCache(nameof(IPlayerDataSource) + nameof(IPlayerDataSource.ReadPlayerIdentities) + "ForTeams" + teamIds);
        }
    }
}
