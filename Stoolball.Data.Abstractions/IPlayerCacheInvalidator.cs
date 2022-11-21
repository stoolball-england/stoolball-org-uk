using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball.Data.Abstractions
{
    public interface IPlayerCacheInvalidator
    {
        void InvalidateCacheForPlayer(Player cacheable);
        void InvalidateCacheForTeams(params Team[] teams);
    }
}