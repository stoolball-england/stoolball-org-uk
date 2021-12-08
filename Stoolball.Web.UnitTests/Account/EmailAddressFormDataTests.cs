using System;
using System.Linq;
using Stoolball.Testing;
using Stoolball.Web.Account;
using Xunit;

namespace Stoolball.Web.UnitTests.Account
{
    public class EmailAddressFormDataTests : ValidationBaseTest
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Invalid_email_address_fails_validation(string email)
        {
            var formData = new EmailAddressFormData
            {
                RequestedEmail = email
            };

            Assert.Contains(ValidateModel(formData),
                v => v.MemberNames.Contains(nameof(EmailAddressFormData.RequestedEmail)) &&
                     v.ErrorMessage.ToUpperInvariant().Contains("EMAIL"));
        }

        [Fact]
        public void Valid_email_passes_validation()
        {
            var formData = new EmailAddressFormData
            {
                RequestedEmail = "email@example.org"
            };

            Assert.DoesNotContain(ValidateModel(formData),
                            v => v.MemberNames.Contains(nameof(EmailAddressFormData.RequestedEmail)) &&
                                 v.ErrorMessage.ToUpperInvariant().Contains("EMAIL"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Invalid_password_fails_validation(string password)
        {
            var formData = new EmailAddressFormData
            {
                Password = password
            };

            Assert.Contains(ValidateModel(formData),
                v => v.MemberNames.Contains(nameof(EmailAddressFormData.Password)) &&
                     v.ErrorMessage.ToUpperInvariant().Contains("PASSWORD"));
        }

        [Fact]
        public void Valid_password_passes_validation()
        {
            var formData = new EmailAddressFormData
            {
                Password = "password"
            };

            Assert.DoesNotContain(ValidateModel(formData),
                            v => v.MemberNames.Contains(nameof(EmailAddressFormData.Password)) &&
                                 v.ErrorMessage.ToUpperInvariant().Contains("PASSWORD"));
        }
    }
}
