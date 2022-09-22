using System;
using System.ComponentModel.DataAnnotations;

namespace Stoolball.Matches
{
    public class OverSet
    {
        public Guid? OverSetId { get; set; }
        public int OverSetNumber { get; set; }

        [Range(1, 999, ErrorMessage = "Innings must be at least 1 over")]
        public int? Overs { get; set; }
        public int? BallsPerOver { get; set; } = 8;
    }
}
