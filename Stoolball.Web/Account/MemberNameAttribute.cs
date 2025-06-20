using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Stoolball.Web.Account
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class MemberNameAttribute : ValidationAttribute
    {
        private static readonly Regex _regex = new Regex(
            "^((?!tronlink|TRONLINK|TronLink).)*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public MemberNameAttribute()
        {
            ErrorMessage = "You cannot create an account with that name";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var name = value as string;
            if (string.IsNullOrEmpty(name))
            {
                return ValidationResult.Success;
            }

            if (name.Contains("http://", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("https://", StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult(ErrorMessage);
            }

            if (_regex.IsMatch(name))
            {
                return ValidationResult.Success;
            }

            return new ValidationResult(ErrorMessage);
        }
    }
}
