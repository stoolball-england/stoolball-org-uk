using System;
using System.Linq;
using Stoolball.Testing;
using Stoolball.Web.Account;
using Xunit;

namespace Stoolball.Web.UnitTests.Account
{
    public class ResetPasswordFormDataTests : ValidationBaseTest
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("isinvalid")]
        public void Invalid_password_fails_validation(string password)
        {
            var formData = new ResetPasswordFormData
            {
                PasswordResetToken = password
            };

            Assert.Contains(ValidateModel(formData),
                v => v.MemberNames.Contains(nameof(ResetPasswordFormData.NewPassword)) &&
                     v.ErrorMessage.ToUpperInvariant() == "THE NEW PASSWORD IS REQUIRED.");
        }

        [Fact]
        public void Valid_password_passes_validation()
        {
            var formData = new ResetPasswordFormData
            {
                NewPassword = "tenlongxxx"
            };

            Assert.DoesNotContain(ValidateModel(formData),
                            v => v.MemberNames.Contains(nameof(ResetPasswordFormData.NewPassword)) &&
                                 v.ErrorMessage.ToUpperInvariant() == "THE NEW PASSWORD IS REQUIRED.");
        }

        [Fact]
        public void Missing_token_fails_validation()
        {
            var formData = new ResetPasswordFormData();

            Assert.Contains(ValidateModel(formData),
                            v => v.MemberNames.Contains(nameof(ResetPasswordFormData.PasswordResetToken)) &&
                                 v.ErrorMessage.ToUpperInvariant() == "THE PASSWORDRESETTOKEN FIELD IS REQUIRED.");
        }

        [Fact]
        public void Valid_token_passes_validation()
        {
            var formData = new ResetPasswordFormData
            {
                PasswordResetToken = Guid.NewGuid().ToString()
            };

            Assert.DoesNotContain(ValidateModel(formData),
                            v => v.MemberNames.Contains(nameof(ResetPasswordFormData.PasswordResetToken)) &&
                                 v.ErrorMessage.ToUpperInvariant() == "THE PASSWORD RESET TOKEN IS REQUIRED.");
        }
    }
}
