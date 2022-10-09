using System.Collections.Generic;

namespace Stoolball.Statistics
{
    public class PlayerEqualityComparer : EqualityComparer<Player>
    {
        public override bool Equals(Player? x, Player? y)
        {
            return x?.PlayerId == y?.PlayerId;
        }

        public override int GetHashCode(Player obj)
        {
            return base.GetHashCode();
        }
    }
}
