using System;
using System.ComponentModel.DataAnnotations;
using Stoolball.Matches;
using Stoolball.Web.Matches.Models;

namespace Stoolball.Web.Matches.Validation
{
    /// <summary>
    /// The batter name is required if any other fields are filled in for an innings
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class BatterRequiredAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var innings = (PlayerInningsViewModel)validationContext.ObjectInstance;

            if (string.IsNullOrWhiteSpace(innings.Batter) &&
                    (innings.DismissalType.HasValue &&
                    innings.DismissalType != DismissalType.DidNotBat ||
                    !string.IsNullOrWhiteSpace(innings.DismissedBy) ||
                    !string.IsNullOrWhiteSpace(innings.Bowler) ||
                    innings.RunsScored != null ||
                    innings.BallsFaced != null))
            {
                return new ValidationResult($"You've added details for a batter with no name. Please name the batter.");
            }
            return null;
        }
    }
}
