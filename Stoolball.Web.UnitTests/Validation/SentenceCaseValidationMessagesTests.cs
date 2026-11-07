using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Stoolball.Web.Validation;
using Xunit;

namespace Stoolball.Web.UnitTests.Validation
{
    public class SentenceCaseValidationMessagesTests
    {
        private class Model
        {
            [Display(Name = "Match date")]
            [Required]
            public string? MatchDate { get; set; }

            [Required]
            public string? Town { get; set; }

            [Display(Name = "Who can play?")]
            [Required(ErrorMessage = "Select who can play.")]
            public string? WhoCanPlay { get; set; }

            [Display(Name = "Street address")]
            [MaxLength(100)]
            public string? StreetAddress { get; set; }

            [Display(Name = "Password")]
            [StringLength(256)]
            public string? Password { get; set; }

            [Display(Name = "Code")]
            [StringLength(10, MinimumLength = 4)]
            public string? Code { get; set; }

            [Display(Name = "Email")]
            [EmailAddress]
            public string? Email { get; set; }

            [DisplayName("Team")]
            [Required]
            public string? TeamName { get; set; }

            [Display(Name = "Twitter account")]
            [Required]
            public string? Twitter { get; set; }

            [Display(Name = "Score {total}")]
            [Required]
            public string? WithBraces { get; set; }

            [Display(Name = "Balls bowled")]
            [Range(1, 12)]
            public int? BallsBowled { get; set; }
        }

        private static string GetMessage(string propertyName)
        {
            var property = typeof(Model).GetProperty(propertyName)!;
            var key = ModelMetadataIdentity.ForProperty(property, property.PropertyType, typeof(Model));
            var attributes = ModelAttributes.GetAttributesForProperty(typeof(Model), property, property.PropertyType);
            var context = new ValidationMetadataProviderContext(key, attributes);
            foreach (var validator in attributes.Attributes.OfType<ValidationAttribute>())
            {
                context.ValidationMetadata.ValidatorMetadata.Add(validator);
            }

            new SentenceCaseValidationMetadataProvider().CreateValidationMetadata(context);

            var formatter = context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>().First();

            // This is how ASP.NET Core formats the message, for both server and client side validation. The display name is the original.
            return formatter.FormatErrorMessage(property.GetCustomAttribute<DisplayAttribute>()?.Name ?? property.Name);
        }

        [Theory]
        [InlineData(nameof(Model.MatchDate), "The match date field is required.")]
        [InlineData(nameof(Model.Town), "The town field is required.")]
        [InlineData(nameof(Model.TeamName), "The team field is required.")]
        [InlineData(nameof(Model.Twitter), "The Twitter account field is required.")]
        [InlineData(nameof(Model.WithBraces), "The score {total} field is required.")]
        [InlineData(nameof(Model.StreetAddress), "The street address must be 100 characters or fewer.")]
        [InlineData(nameof(Model.Password), "The password must be 256 characters or fewer.")]
        [InlineData(nameof(Model.Code), "The code must be between 4 and 10 characters.")]
        [InlineData(nameof(Model.Email), "Enter a valid email address.")]
        [InlineData(nameof(Model.BallsBowled), "The field balls bowled must be between 1 and 12.")]
        public void Default_messages_use_sentence_case(string propertyName, string expected)
        {
            Assert.Equal(expected, GetMessage(propertyName));
        }

        [Fact]
        public void Explicit_error_messages_are_not_changed()
        {
            Assert.Equal("Select who can play.", GetMessage(nameof(Model.WhoCanPlay)));
        }

        [Fact]
        public void Number_and_invalid_value_messages_use_sentence_case()
        {
            var options = new MvcOptions();

            options.UseSentenceCaseValidationMessages();

            Assert.Equal("The field points for home team must be a number.", options.ModelBindingMessageProvider.ValueMustBeANumberAccessor("Points for home team"));
            Assert.Equal("The value 'x' is not valid for match date.", options.ModelBindingMessageProvider.AttemptedValueIsInvalidAccessor("x", "Match date"));
        }
    }
}
