using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Matches;
using Stoolball.Web.Matches.ModelBinders;
using Stoolball.Web.Matches.Validation;

namespace Stoolball.Web.Matches.Models
{
    [ModelBinder(BinderType = typeof(PlayerInningsModelBinder))]
    public class PlayerInningsViewModel
    {
        [BatterRequired]
        public string? Batter { get; set; }

        [DidNotBatCannotHaveBattingDetails]
        [NotOutCannotHaveDismissalDetails]
        [Display(Name = "How out")]
        public DismissalType? DismissalType { get; set; }

        [DismissalTypeCannotHaveDismissedBy]
        [Display(Name = "Caught/run-out by")]
        public string? DismissedBy { get; set; }

        [RunOutCannotHaveBowler]
        public string? Bowler { get; set; }

        [Display(Name = "Runs")]
        public int? RunsScored { get; set; }

        [Display(Name = "Balls")]
        [Range(0, 1000000, ErrorMessage = "Balls faced must be a number, 0 or more")]
        public int? BallsFaced { get; set; }
    }
}