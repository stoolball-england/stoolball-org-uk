using System;
using System.ComponentModel.DataAnnotations;
using Stoolball.Matches;
using Stoolball.Web.Matches.Models;

namespace Stoolball.Web.Matches.Validation
{
    /// <summary>
    /// The batter must have batted if any other fields are filled in for an innings
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DidNotBatCannotHaveBattingDetailsAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var innings = (PlayerInningsViewModel)validationContext.ObjectInstance;

            if ((innings.DismissalType == DismissalType.DidNotBat || innings.DismissalType == DismissalType.TimedOut) &&
                    (!string.IsNullOrWhiteSpace(innings.DismissedBy) ||
                    !string.IsNullOrWhiteSpace(innings.Bowler) ||
                    innings.RunsScored != null ||
                    innings.BallsFaced != null))
            {
                return new ValidationResult($"You've said {innings.Batter} did not bat, but you added batting details.", new[] { validationContext.MemberName! });
            }
            return null;
        }
    }
}
