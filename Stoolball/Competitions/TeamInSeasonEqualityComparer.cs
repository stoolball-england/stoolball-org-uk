using System.Collections.Generic;

namespace Stoolball.Competitions
{
    public class TeamInSeasonEqualityComparer : EqualityComparer<TeamInSeason>
    {
        public override bool Equals(TeamInSeason x, TeamInSeason y)
        {
            return x?.Season.SeasonId == y?.Season.SeasonId;
        }

        public override int GetHashCode(TeamInSeason obj)
        {
            return base.GetHashCode();
        }
    }
}
