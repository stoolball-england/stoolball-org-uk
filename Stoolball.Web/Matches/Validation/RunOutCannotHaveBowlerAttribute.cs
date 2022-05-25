using System;
using System.ComponentModel.DataAnnotations;
using Stoolball.Matches;
using Stoolball.Web.Matches.Models;

namespace Stoolball.Web.Matches.Validation
{
    /// <summary>
    /// The bowler is not credited when a batter is run-out
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RunOutCannotHaveBowlerAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var innings = (PlayerInningsViewModel)validationContext.ObjectInstance;

            if ((innings.DismissalType == DismissalType.RunOut) && !string.IsNullOrWhiteSpace(innings.Bowler))
            {
                return new ValidationResult($"You've said {innings.Batter} was run-out, but you named a bowler. A run-out is credited to the fielder.", new[] { validationContext.MemberName! });
            }
            return null;
        }
    }
}
