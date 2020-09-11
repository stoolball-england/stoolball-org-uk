using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Stoolball.Audit;
using Stoolball.Teams;

namespace Stoolball.Matches
{
    public class Over : IAuditable
    {
        public Guid? OverId { get; set; }

        public Match Match { get; set; }

        public PlayerIdentity PlayerIdentity { get; set; }

        public int OverNumber { get; set; }

        [Range(1, 12)]
        [Display(Name = "balls bowled")]
        public int? BallsBowled { get; set; }

        [Display(Name = "no balls")]
        [Range(0, 1000, ErrorMessage = "The field no balls must be 0 or more.")]
        public int? NoBalls { get; set; }

        [Display(Name = "wides")]
        [Range(0, 1000, ErrorMessage = "The field wides must be 0 or more.")]
        public int? Wides { get; set; }

        [Display(Name = "over total")]
        [Range(0, 1000, ErrorMessage = "The field over total must be 0 or more.")]
        public int? RunsConceded { get; set; }
        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();
        public Uri EntityUri {
            get { return new Uri($"https://www.stoolball.org.uk/id/over/{OverId}"); }
        }
    }
}