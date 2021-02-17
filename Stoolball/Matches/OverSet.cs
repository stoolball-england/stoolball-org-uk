using System;

namespace Stoolball.Matches
{
    public class OverSet
    {
        public Guid? OverSetId { get; set; }
        public int OverSetNumber { get; set; }
        public int? Overs { get; set; }
        public int? BallsPerOver { get; set; } = 8;
    }
}
