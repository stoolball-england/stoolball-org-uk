using System;
using System.ComponentModel.DataAnnotations;
using Stoolball.Matches;

namespace Stoolball.Competitions
{
    public class PointsRule
    {
        public Guid PointsRuleId { get; set; }
        public MatchResultType MatchResultType { get; set; }

        [Required]
        [Display(Name = "Points for home team")]
        public int HomePoints { get; set; }

        [Required]
        [Display(Name = "Points for away team")]
        public int AwayPoints { get; set; }

        /// <summary>
        /// Creates a copy of this <c>PointsRule</c>.
        /// </summary>
        public PointsRule Clone()
        {
            return new PointsRule
            {
                PointsRuleId = PointsRuleId,
                MatchResultType = MatchResultType,
                HomePoints = HomePoints,
                AwayPoints = AwayPoints
            };
        }
    }
}