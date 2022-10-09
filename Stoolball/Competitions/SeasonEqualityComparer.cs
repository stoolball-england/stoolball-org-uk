using System.Collections.Generic;

namespace Stoolball.Competitions
{
    public class SeasonEqualityComparer : EqualityComparer<Season>
    {
        public override bool Equals(Season? x, Season? y)
        {
            return x?.SeasonRoute == y?.SeasonRoute;
        }

        public override int GetHashCode(Season obj)
        {
            return base.GetHashCode();
        }
    }
}
