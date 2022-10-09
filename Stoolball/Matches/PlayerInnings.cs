using System;
using System.Collections.Generic;
using Stoolball.Logging;
using Stoolball.Statistics;

namespace Stoolball.Matches
{
    public class PlayerInnings : IAuditable
    {
        public Guid? PlayerInningsId { get; set; }

        public Match? Match { get; set; }

        public PlayerIdentity? Batter { get; set; }

        public int BattingPosition { get; set; }

        public DismissalType? DismissalType { get; set; }

        public PlayerIdentity? DismissedBy { get; set; }

        public PlayerIdentity? Bowler { get; set; }

        public int? RunsScored { get; set; }

        public int? BallsFaced { get; set; }
        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();
        public Uri EntityUri {
            get { return new Uri($"https://www.stoolball.org.uk/id/player-innings/{PlayerInningsId}"); }
        }
    }
}