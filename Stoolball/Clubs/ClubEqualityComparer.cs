using System.Collections.Generic;

namespace Stoolball.Clubs
{
    public class ClubEqualityComparer : EqualityComparer<Club>
    {
        public override bool Equals(Club? x, Club? y)
        {
            if (x?.ClubId != null && y?.ClubId != null)
            {
                return x.ClubId.Value == y.ClubId.Value;
            }
            else if (!string.IsNullOrEmpty(x?.ClubRoute) && !string.IsNullOrEmpty(y?.ClubRoute))
            {
                return x.ClubRoute.Equals(y.ClubRoute);
            }
            return false;
        }

        public override int GetHashCode(Club obj)
        {
            return base.GetHashCode();
        }
    }
}
