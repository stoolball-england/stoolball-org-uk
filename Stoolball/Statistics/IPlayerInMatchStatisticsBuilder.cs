using System.Collections.Generic;
using Stoolball.Matches;

namespace Stoolball.Statistics
{
    public interface IPlayerInMatchStatisticsBuilder
    {
        IEnumerable<PlayerInMatchStatisticsRecord> BuildStatisticsForMatch(Match match);
    }
}