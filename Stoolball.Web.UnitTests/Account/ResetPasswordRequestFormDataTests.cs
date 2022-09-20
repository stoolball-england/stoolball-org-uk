using System;
using System.Linq;
using Stoolball.Testing;
using Stoolball.Web.Account;
using Xunit;

namespace Stoolball.Web.UnitTests.Account
{
    public class ResetPasswordRequestFormDataTests : ValidationBaseTest
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("invalid")]
        public void Invalid_email_address_fails_validation(string email)
        {
            var formData = new ResetPasswordRequestFormData
            {
                Email = email
            };

            Assert.Contains(ValidateModel(formData),
                v => v.MemberNames.Contains(nameof(ResetPasswordRequestFormData.Email)) &&
                     (v.ErrorMessage?.ToUpperInvariant().Contains("EMAIL") ?? false));
        }

        [Fact]
        public void Valid_email_passes_validation()
        {
            var formData = new ResetPasswordRequestFormData
            {
                Email = "email@example.org"
            };

            Assert.DoesNotContain(ValidateModel(formData),
                            v => v.MemberNames.Contains(nameof(ResetPasswordRequestFormData.Email)) &&
                                 (v.ErrorMessage?.ToUpperInvariant().Contains("EMAIL") ?? false));
        }
    }
}
