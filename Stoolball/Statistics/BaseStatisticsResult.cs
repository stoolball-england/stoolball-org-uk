using Stoolball.Matches;
using Stoolball.Teams;

namespace Stoolball.Statistics
{
    public abstract class BaseStatisticsResult
    {
        public Player Player { get; set; }
        public Team Team { get; set; }
        public Team OppositionTeam { get; set; }
        public MatchListing Match { get; set; }
    }
}
