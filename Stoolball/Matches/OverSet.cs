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

        /// <summary>
        /// Creates a copy of this <c>OverSet</c>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public OverSet Clone()
        {
            return new OverSet
            {
                OverSetId = OverSetId,
                OverSetNumber = OverSetNumber,
                Overs = Overs,
                BallsPerOver = BallsPerOver
            };
        }
    }
}
