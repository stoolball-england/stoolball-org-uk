using System.Collections.Generic;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball.Data.Abstractions
{
    public interface IPlayerCacheInvalidator
    {
        /// <summary>
        /// Invalidate the cache for a single player, including their key statistics (but not all statistics).
        /// </summary>
        /// <param name="player">The player to clear the cache for.</param>
        void InvalidateCacheForPlayer(Player player);

        /// <summary>
        /// Invalidate the combined cache of player identities for all of the teams playing in a match.
        /// </summary>
        /// <param name="teamsInMatch">The teams playing in the match.</param>
        void InvalidateCacheForTeams(IEnumerable<TeamInMatch> teamsInMatch);

        /// <summary>
        /// Invalidate the individual cache of player identities for each supplied team.
        /// </summary>
        /// <param name="teams">The teams for which individual caches should be cleared.</param>
        void InvalidateCacheForTeams(params Team[] teams);
    }
}