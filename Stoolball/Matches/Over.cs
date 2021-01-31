using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Stoolball.Logging;
using Stoolball.Teams;

namespace Stoolball.Matches
{
    public class Over : IAuditable
    {
        public Guid? OverId { get; set; }

        public PlayerIdentity PlayerIdentity { get; set; }

        public int OverNumber { get; set; }

        public int? BallsPerOver { get; set; } = 8;

        [Range(1, 12, ErrorMessage = "Balls bowled must be between 1 and 12")]
        [Display(Name = "Balls bowled")]
        public int? BallsBowled { get; set; }

        [Display(Name = "No balls")]
        [Range(0, 1000, ErrorMessage = "No balls must be a number, 0 or more.")]
        public int? NoBalls { get; set; }

        [Display(Name = "Wides")]
        [Range(0, 1000, ErrorMessage = "Wides must be a number, 0 or more.")]
        public int? Wides { get; set; }

        [Display(Name = "Over total")]
        [Range(0, 1000, ErrorMessage = "Over total must be a number, 0 or more.")]
        public int? RunsConceded { get; set; }
        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();
        public Uri EntityUri {
            get { return new Uri($"https://www.stoolball.org.uk/id/over/{OverId}"); }
        }
    }
}