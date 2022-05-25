using System;
using System.ComponentModel.DataAnnotations;
using Stoolball.Matches;
using Stoolball.Web.Matches.Models;

namespace Stoolball.Web.Matches.Validation
{
    /// <summary>
    /// The batter can't be not out if a a bowler or fielder is named
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class NotOutCannotHaveDismissalDetailsAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var innings = (PlayerInningsViewModel)validationContext.ObjectInstance;

            if ((innings.DismissalType == DismissalType.NotOut || innings.DismissalType == DismissalType.Retired || innings.DismissalType == DismissalType.RetiredHurt) &&
                       (!string.IsNullOrWhiteSpace(innings.DismissedBy) ||
                       !string.IsNullOrWhiteSpace(innings.Bowler)
                       ))
            {
                return new ValidationResult($"You've said {innings.Batter} was not out, but you named a fielder and/or bowler.");
            }
            return null;
        }
    }
}
