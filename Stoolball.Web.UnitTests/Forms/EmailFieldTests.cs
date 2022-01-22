using Stoolball.Web.App_Plugins.Stoolball.UmbracoForms;
using Xunit;

namespace Stoolball.Web.UnitTests.Forms
{
    public class EmailFieldTests
    {
        [Theory]
        [InlineData("just a name")]
        [InlineData("first.last@example.org.")]
        [InlineData("first.last.@example.org")]
        [InlineData("first.last@example..org")]
        [InlineData("first.last@o'no.org")]
        public void InvalidEmailIsRejected(string email)
        {
            var validator = new EmailField();

            var isValid = validator.ValidateEmailAddress(email);

            Assert.False(isValid);
        }

        [Theory]
        [InlineData("first.last@example.org")]
        [InlineData("first.o'yes@example.org")]
        public void ValidEmailIsAccepted(string email)
        {
            var validator = new EmailField();

            var isValid = validator.ValidateEmailAddress(email);

            Assert.True(isValid);
        }
    }
}
