using Stoolball.Audit;
using Stoolball.Teams;
using System;
using System.Collections.Generic;

namespace Stoolball.Matches
{
    public class Batting : IAuditable
    {
        public Guid? BattingId { get; set; }

        public Match Match { get; set; }

        public PlayerIdentity PlayerIdentity { get; set; }

        public int BattingPosition { get; set; }

        public DismissalType? HowOut { get; set; }

        public PlayerIdentity DismissedBy { get; set; }

        public PlayerIdentity Bowler { get; set; }

        public int? RunsScored { get; set; }

        public int? BallsFaced { get; set; }
        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();
        public Uri EntityUri
        {
            get { return new Uri($"https://www.stoolball.org.uk/id/batting/{BattingId}"); }
        }
    }
}