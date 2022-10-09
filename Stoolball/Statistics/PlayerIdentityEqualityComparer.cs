using System.Collections.Generic;

namespace Stoolball.Statistics
{
    public class PlayerIdentityEqualityComparer : EqualityComparer<PlayerIdentity>
    {
        public override bool Equals(PlayerIdentity? x, PlayerIdentity? y)
        {
            return x?.PlayerIdentityId == y?.PlayerIdentityId;
        }

        public override int GetHashCode(PlayerIdentity obj)
        {
            return base.GetHashCode();
        }
    }
}
