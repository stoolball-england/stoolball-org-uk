using Stoolball.Email;
using Xunit;

namespace Stoolball.UnitTests.Emails
{
    public class ContactDetailsParserTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("There is no email address here")]
        [InlineData("<p>There is no email address here</p>")]
        public void No_email_returns_null(string html)
        {
            var parser = new ContactDetailsParser();

            var result = parser.ParseFirstEmailAddress(html);

            Assert.Null(result);
        }

        [Fact]
        public void First_email_is_parsed_from_text()
        {
            var parser = new ContactDetailsParser();
            var text = "This text has first.address@example.org in it and secondaddress@example.org.uk as well.";

            var result = parser.ParseFirstEmailAddress(text);

            Assert.Equal("first.address@example.org", result);
        }

        [Fact]
        public void First_email_is_parsed_from_mailto()
        {
            var parser = new ContactDetailsParser();
            var text = "<p>This text has <a href=\"mailto:first.address@example.org\">first.address@example.org</a> in it and secondaddress@example.org.uk as well.</p>";

            var result = parser.ParseFirstEmailAddress(text);

            Assert.Equal("first.address@example.org", result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("There is no phone number here")]
        [InlineData("<p>There is no phone nubmer here</p>")]
        public void No_phone_returns_null(string html)
        {
            var parser = new ContactDetailsParser();

            var result = parser.ParseFirstPhoneNumber(html);

            Assert.Null(result);
        }


        [Theory]
        [InlineData("This mobile number 07891 765432 should be parsed", "07891 765432")]
        [InlineData("This mobile number 0789 176 5432 should be parsed", "0789 176 5432")]
        [InlineData("This punctuated number (01234) 123456 be parsed", "01234 123456")]
        [InlineData("This London number <strong>020 7976 3900</strong> for the Sport & Recreation Alliance should be parsed", "020 7976 3900")]
        [InlineData("<p>This Sport England number 0345 8508 508 should be parsed, not 01234 567891 which comes later</p>", "0345 8508 508")]
        public void First_phone_is_parsed(string html, string expected)
        {
            var parser = new ContactDetailsParser();

            var result = parser.ParseFirstPhoneNumber(html);

            Assert.Equal(expected, result);
        }
    }
}
