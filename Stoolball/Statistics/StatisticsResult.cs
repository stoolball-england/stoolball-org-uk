using Stoolball.Matches;
using Stoolball.Teams;

namespace Stoolball.Statistics
{
    public class StatisticsResult<T>
    {
        public Player Player { get; set; }
        public Team Team { get; set; }
        public Team OppositionTeam { get; set; }
        public MatchListing Match { get; set; }
        public T Result { get; set; }
    }
}
