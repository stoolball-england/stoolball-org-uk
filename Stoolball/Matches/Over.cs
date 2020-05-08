using Stoolball.Audit;
using Stoolball.Teams;
using System;
using System.Collections.Generic;

namespace Stoolball.Matches
{
    public class Over : IAuditable
    {
        public Guid? OverId { get; set; }

        public Match Match { get; set; }

        public PlayerIdentity PlayerIdentity { get; set; }

        public int OverNumber { get; set; }

        public int? BallsBowled { get; set; }

        public int? NoBalls { get; set; }

        public int? Wides { get; set; }

        public int? RunsConceded { get; set; }
        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();
        public Uri EntityUri
        {
            get { return new Uri($"https://www.stoolball.org.uk/id/over/{OverId}"); }
        }
    }
}