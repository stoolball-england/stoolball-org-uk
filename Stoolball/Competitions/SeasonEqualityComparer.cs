using System.Collections.Generic;

namespace Stoolball.Competitions
{
    public class SeasonEqualityComparer : EqualityComparer<Season>
    {
        public override bool Equals(Season? x, Season? y)
        {
            if (x?.SeasonId != null && y?.SeasonId != null)
            {
                return x.SeasonId.Value == y.SeasonId.Value;
            }
            else if (!string.IsNullOrEmpty(x?.SeasonRoute) && !string.IsNullOrEmpty(y?.SeasonRoute))
            {
                return x.SeasonRoute.Equals(y.SeasonRoute);
            }
            return false;
        }

        public override int GetHashCode(Season obj)
        {
            return base.GetHashCode();
        }
    }
}
