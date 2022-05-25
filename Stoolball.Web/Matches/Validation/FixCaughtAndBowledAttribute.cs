using System;
using System.ComponentModel.DataAnnotations;
using Stoolball.Matches;
using Stoolball.Web.Matches.Models;

namespace Stoolball.Web.Matches.Validation
{
    /// <summary>
    /// Caught and bowled by the same person is caught and bowled
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class FixCaughtAndBowledAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var innings = (PlayerInningsViewModel)validationContext.ObjectInstance;

            if (innings.DismissalType == DismissalType.Caught &&
                    !string.IsNullOrWhiteSpace(innings.DismissedBy) &&
                    innings.DismissedBy?.Trim() == innings.Bowler?.Trim())
            {
                innings.DismissalType = DismissalType.CaughtAndBowled;
                innings.DismissedBy = null;
            }
            return null;
        }
    }
}
