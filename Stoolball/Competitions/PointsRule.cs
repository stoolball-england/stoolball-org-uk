using Stoolball.Matches;

namespace Stoolball.Competitions
{
    public class PointsRule
    {
        public MatchResultType MatchResultType { get; set; }
        public int HomePoints { get; set; }
        public int AwayPoints { get; set; }
    }
}