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
        [InlineData(null, "THE NEW PASSWORD IS REQUIRED.")]
        [InlineData("", "THE NEW PASSWORD IS REQUIRED.")]
        [InlineData("isinvalid", "YOUR PASSWORD MUST BE AT LEAST 10 CHARACTERS")]
        public void Invalid_password_fails_validation(string password, string expectedError)
        {
            var formData = new ResetPasswordFormData
            {
                NewPassword = password
            };

            Assert.Contains(ValidateModel(formData),
                v => v.MemberNames.Contains(nameof(ResetPasswordFormData.NewPassword)) &&
                     v.ErrorMessage.ToUpperInvariant() == expectedError);
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
    }
}
