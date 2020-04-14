using Stoolball.Email;
using Xunit;

namespace Stoolball.Tests.Emails
{
    public class EmailProtectorTests
    {
        [Theory]
        [InlineData("<p>email@example.org</p>", false, @"<p>(email address available – please <a href=""/account/sign-in"">sign in</a>)</p>")]
        [InlineData("<p>email@example.org</p>", true, @"<p><a href=""&#0109;&#0097;&#0105;&#0108;&#0116;&#0111;&#0058;&#0101;&#0109;&#0097;&#0105;&#0108;&#0064;&#0101;&#0120;&#0097;&#0109;&#0112;&#0108;&#0101;&#0046;&#0111;&#0114;&#0103;"">&#0101;&#0109;&#0097;&#0105;&#0108;&#0064;&#0101;&#0120;&#0097;&#0109;&#0112;&#0108;&#0101;&#0046;&#0111;&#0114;&#0103;</a></p>")]
        public void Email_is_protected(string input, bool userIsAuthenticated, string expectedOutput)
        {
            var protector = new EmailProtector();

            var result = protector.ProtectEmailAddresses(input, userIsAuthenticated);

            Assert.Equal(expectedOutput, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Null_or_empty_is_returned_unaltered(string input)
        {
            var protector = new EmailProtector();

            var result = protector.ProtectEmailAddresses(input, true);

            Assert.Equal(input, result);
        }
    }
}
