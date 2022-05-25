using System;
using System.ComponentModel.DataAnnotations;
using Stoolball.Matches;
using Stoolball.Web.Matches.Models;

namespace Stoolball.Web.Matches.Validation
{
    /// <summary>
    /// If there's a fielder, the dismissal type should be caught or run-out
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DismissalTypeCannotHaveDismissedByAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var innings = (PlayerInningsViewModel)validationContext.ObjectInstance;

            if (innings.DismissalType != DismissalType.Caught &&
                    innings.DismissalType != DismissalType.RunOut &&
                    !string.IsNullOrWhiteSpace(innings.DismissedBy))
            {
                return new ValidationResult($"You've named the fielder who dismissed {innings.Batter}, but they were not caught or run-out.");
            }
            return null;
        }
    }
}
