using Stoolball.Email;
using Xunit;

namespace Stoolball.Tests.Emails
{
    public class EmailProtectorTests
    {
        private const string OBFUSCATED_MAILTO = "&#0109;&#0097;&#0105;&#0108;&#0116;&#0111;&#0058;";
        private const string OBFUSCATED_EMAIL_ADDRESS = "&#0101;&#0109;&#0097;&#0105;&#0108;&#0064;&#0101;&#0120;&#0097;&#0109;&#0112;&#0108;&#0101;&#0046;&#0111;&#0114;&#0103;";

        [Theory]
        [InlineData("<p>Before email@example.org after</p>")]
        [InlineData(@"<p>Before <a href=""mailto:email@example.org"">email@example.org</a> after</p>")]
        [InlineData(@"<p>Before <a href=""mailto:email@example.org"">send an email</a> after</p>")]
        public void Email_is_protected_if_user_is_unauthenticated(string input)
        {
            var protector = new EmailProtector();

            var result = protector.ProtectEmailAddresses(input, false);

            Assert.Equal(@"<p>Before (email address available – please <a href=""/account/sign-in"">sign in</a>) after</p>", result);
        }

        [Theory]
        [InlineData("<p>Before email@example.org after</p>", OBFUSCATED_EMAIL_ADDRESS)]
        [InlineData(@"<p>Before <a href=""mailto:email@example.org"">email@example.org</a> after</p>", OBFUSCATED_EMAIL_ADDRESS)]
        [InlineData(@"<p>Before <a href=""mailto:email@example.org"">send an email</a> after</p>", "send an email")]
        public void Email_is_obfuscated_if_user_is_authenticated(string input, string expectedLinkText)
        {
            var protector = new EmailProtector();

            var result = protector.ProtectEmailAddresses(input, true);

            Assert.Equal(@$"<p>Before <a href=""{OBFUSCATED_MAILTO + OBFUSCATED_EMAIL_ADDRESS}"">{expectedLinkText}</a> after</p>", result);
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
