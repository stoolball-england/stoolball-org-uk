using System;
using System.ComponentModel.DataAnnotations;

namespace Stoolball.Matches
{
    public class OverSet : ICloneable
    {
        public Guid? OverSetId { get; set; }
        public int OverSetNumber { get; set; }

        [Range(1, 999, ErrorMessage = "Innings must be at least 1 over")]
        public int? Overs { get; set; }
        public int? BallsPerOver { get; set; } = 8;

        /// <summary>
        /// Creates a copy of this <c>OverSet</c>
        /// </summary>
        /// <returns></returns>
        public OverSet Clone() => (OverSet)MemberwiseClone();

        /// <summary>
        /// Creates a copy of this <c>OverSet</c>
        /// </summary>
        /// <returns></returns>
        object ICloneable.Clone() => Clone();
    }
}
