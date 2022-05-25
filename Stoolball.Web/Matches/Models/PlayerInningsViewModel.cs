using System.ComponentModel.DataAnnotations;
using Stoolball.Matches;
using Stoolball.Web.Matches.Validation;

namespace Stoolball.Web.Matches.Models
{
    public class PlayerInningsViewModel
    {
        [BatterRequired]
        public string? Batter { get; set; }

        [DidNotBatCannotHaveBattingDetails]
        [DismissalTypeCannotHaveDismissedBy]
        [NotOutCannotHaveDismissalDetails]
        [FixCaughtAndBowled]
        [Display(Name = "How out")]
        public DismissalType? DismissalType { get; set; }

        [Display(Name = "Caught/run-out by")]
        public string? DismissedBy { get; set; }

        public string? Bowler { get; set; }

        [Display(Name = "Runs")]
        public int? RunsScored { get; set; }

        [Display(Name = "Balls")]
        [Range(0, 1000000, ErrorMessage = "Balls faced must be a number, 0 or more")]
        public int? BallsFaced { get; set; }
    }
}