using System;
using System.Collections.Generic;
using System.Linq;

namespace Stoolball.Statistics
{
    public class PlayerIdentityList : List<PlayerIdentity>
    {
        public PlayerIdentityList() : base() { }
        public PlayerIdentityList(IEnumerable<PlayerIdentity> playerIdentities) : base()
        {
            foreach (var identity in playerIdentities) { Add(identity); }
        }

        public new void Add(PlayerIdentity playerIdentity)
        {
            if (playerIdentity.PlayerIdentityId.HasValue &&
                this.Any(pi => pi.PlayerIdentityId == playerIdentity.PlayerIdentityId))
            {
                throw new InvalidOperationException($"{nameof(PlayerIdentity)} {playerIdentity.PlayerIdentityId} has already been added");
            }
            base.Add(playerIdentity);
        }
    }
}
