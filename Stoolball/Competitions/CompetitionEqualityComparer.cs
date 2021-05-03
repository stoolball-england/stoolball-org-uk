using System.Collections.Generic;

namespace Stoolball.Competitions
{
    public class CompetitionEqualityComparer : EqualityComparer<Competition>
    {
        public override bool Equals(Competition x, Competition y)
        {
            return x?.CompetitionId == y?.CompetitionId;
        }

        public override int GetHashCode(Competition obj)
        {
            return base.GetHashCode();
        }
    }
}
