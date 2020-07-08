using Stoolball.Matches;
using System;
using System.ComponentModel.DataAnnotations;

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
    }
}