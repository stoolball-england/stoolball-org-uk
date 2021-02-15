using System.Collections.Generic;
using Stoolball.Matches;

namespace Stoolball.Statistics
{
    public class PlayerInMatchStatisticsBuilder : IPlayerInMatchStatisticsBuilder
    {
        public IEnumerable<PlayerInMatchStatisticsRecord> BuildStatisticsForMatch(Match match)
        {
            return new List<PlayerInMatchStatisticsRecord>();
        }
    }
}
