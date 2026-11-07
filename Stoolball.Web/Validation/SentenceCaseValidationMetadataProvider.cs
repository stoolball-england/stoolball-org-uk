using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Stoolball.Web.Validation
{
    /// <summary>
    /// Replaces the default error messages of data annotation validators with ones that use sentence case, such as "The match date field is required."
    /// </summary>
    /// <remarks>
    /// The default messages insert the display name of the property in the middle of the sentence, but display names are written for labels so start with a capital letter.
    /// Validators with their own <see cref="ValidationAttribute.ErrorMessage" /> are left alone. See docs/Validation.md.
    /// </remarks>
    public class SentenceCaseValidationMetadataProvider : IValidationMetadataProvider
    {
        private const string NamePlaceholder = "\u0001";

        public void CreateValidationMetadata(ValidationMetadataProviderContext context)
        {
            if (context is null)
            {
                throw new System.ArgumentNullException(nameof(context));
            }

            if (context.Key.MetadataKind != ModelMetadataKind.Property)
            {
                return;
            }

            var displayName = SentenceCase.ToMidSentence(GetDisplayName(context));

            foreach (var validator in context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
            {
                if (validator.ErrorMessageResourceName is not null || (validator.ErrorMessage is not null && !HasDefaultMessageOnly(validator)))
                {
                    continue;
                }

                var message = CreateMessage(validator, displayName);
                if (message is not null)
                {
                    // ErrorMessage is a format string, so escape any braces. This also stops it being formatted twice.
                    validator.ErrorMessage = message.Replace("{", "{{").Replace("}", "}}");
                }
            }
        }

        /// <summary>
        /// <see cref="EmailAddressAttribute" /> and other <see cref="DataTypeAttribute" /> validators set a default message, which is returned by <see cref="ValidationAttribute.ErrorMessage" /> as if it was set explicitly.
        /// </summary>
        private static bool HasDefaultMessageOnly(ValidationAttribute validator)
        {
            return validator is EmailAddressAttribute && validator.ErrorMessage == new EmailAddressAttribute().ErrorMessage;
        }

        private static string GetDisplayName(ValidationMetadataProviderContext context)
        {
            return context.Attributes.OfType<DisplayAttribute>().FirstOrDefault()?.GetName()
                ?? context.Attributes.OfType<DisplayNameAttribute>().FirstOrDefault()?.DisplayName
                ?? context.Key.Name
                ?? string.Empty;
        }

        private static string? CreateMessage(ValidationAttribute validator, string displayName)
        {
            switch (validator)
            {
                case MaxLengthAttribute maxLength:
                    return CharactersOrFewer(displayName, maxLength.Length);
                case StringLengthAttribute stringLength when stringLength.MinimumLength > 0:
                    return $"The {displayName} must be between {stringLength.MinimumLength} and {stringLength.MaximumLength} characters.";
                case StringLengthAttribute stringLength:
                    return CharactersOrFewer(displayName, stringLength.MaximumLength);
                case EmailAddressAttribute:
                    return "Enter a valid email address.";
                case RequiredAttribute:
                case RangeAttribute:
                case RegularExpressionAttribute:
                    // Take the default message from the framework rather than copying it, then put the display name in the placeholder.
                    return validator.FormatErrorMessage(NamePlaceholder).Replace(NamePlaceholder, displayName);
                default:
                    return null;
            }
        }

        private static string CharactersOrFewer(string displayName, int length)
        {
            return $"The {displayName} must be {length} {(length == 1 ? "character" : "characters")} or fewer.";
        }
    }
}
