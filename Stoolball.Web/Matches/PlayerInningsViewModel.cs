﻿using System.ComponentModel.DataAnnotations;
using Stoolball.Matches;

namespace Stoolball.Web.Matches
{
    public class PlayerInningsViewModel
    {
        public string Batter { get; set; }

        [Display(Name = "How out")]
        public DismissalType? DismissalType { get; set; }

        [Display(Name = "Caught/run-out by")]
        public string DismissedBy { get; set; }

        public string Bowler { get; set; }

        [Display(Name = "Runs")]
        [Range(0, 1000000, ErrorMessage = "Runs must be a number, 0 or more")]
        public int? RunsScored { get; set; }

        [Display(Name = "Balls")]
        [Range(0, 1000000, ErrorMessage = "Balls faced must be a number, 0 or more")]
        public int? BallsFaced { get; set; }
    }
}