using System;
using System.Collections.Generic;

namespace Stoolball.Testing
{
    public class MemberEqualityComparer : EqualityComparer<(Guid memberId, string)>
    {
        public override bool Equals((Guid memberId, string) x, (Guid memberId, string) y)
        {
            return x.memberId == y.memberId;
        }

        public override int GetHashCode((Guid, string) obj)
        {
            return base.GetHashCode();
        }
    }
}
