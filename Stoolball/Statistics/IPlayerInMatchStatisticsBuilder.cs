using System.Collections.Generic;
using Stoolball.Matches;

namespace Stoolball.Statistics
{
    public interface IPlayerInMatchStatisticsBuilder
    {
        /// <summary>
        /// Builds statistics for a player in any kind of match except a training session, including batting, bowling, and fielding records.
        /// </summary>
        /// <param name="match">The match.</param>
        /// <returns>Records suitable for querying for player statistics.</returns>
        IEnumerable<PlayerInMatchStatisticsRecord> BuildStatisticsForMatch(Match match);
    }
}