using Stoolball.Teams;

namespace Stoolball.Statistics
{
    public interface IPlayerCacheClearer
    {
        void ClearCacheForPlayer(Player cacheable);
        void ClearCacheForTeams(params Team[] teams);
    }
}