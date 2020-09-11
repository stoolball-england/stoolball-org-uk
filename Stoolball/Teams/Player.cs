using System;
using System.Collections.Generic;
using Stoolball.Audit;

namespace Stoolball.Teams
{
    public class Player : IAuditable
    {
        public Guid? PlayerId { get; set; }

        public string PlayerName { get; set; }

        public PlayerRole PlayerRole { get; set; }

        public string PlayerRoute { get; set; }

        public List<PlayerIdentity> PlayerIdentities { get; internal set; } = new List<PlayerIdentity>();

        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();

        public Uri EntityUri {
            get { return new Uri($"https://www.stoolball.org.uk/id/player/{PlayerId}"); }
        }
    }
}
