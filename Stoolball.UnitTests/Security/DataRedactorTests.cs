using Stoolball.Security;
using Xunit;

namespace Stoolball.UnitTests.Security
{
    public class DataRedactorTests
    {
        [Fact]
        public void RedactAll_replaces_all_alphanumeric()
        {
            var before = "This is a 123 test";
            var after = "**** ** * *** ****";
            var redactor = new DataRedactor();

            var result = redactor.RedactAll(before);

            Assert.Equal(after, result);
        }

        [Fact]
        public void RedactAll_does_not_affect_HTML_tags()
        {
            var before = "<p>This is a 123 test</p>";
            var after = "<p>**** ** * *** ****</p>";
            var redactor = new DataRedactor();

            var result = redactor.RedactAll(before);

            Assert.Equal(after, result);
        }

        [Fact]
        public void RedactPersonalData_does_not_affect_all_alphanumeric()
        {
            var safeData = "This is a 123 test";
            var redactor = new DataRedactor();

            var result = redactor.RedactPersonalData(safeData);

            Assert.Equal(safeData, result);
        }


        [Theory]
        [InlineData("test@example.org")]
        [InlineData("test@newer-domain.design")]
        public void RedactPersonalData_replaces_email_address(string emailAddress)
        {
            var before = $"Email {emailAddress} to test";
            var after = "Email *****@*****.*** to test";
            var redactor = new DataRedactor();

            var result = redactor.RedactPersonalData(before);

            Assert.Equal(after, result);
        }

        [Theory]
        [InlineData("07891123456")]
        [InlineData("07891 123456")]
        [InlineData("(01234) 123456")]
        [InlineData("020 123 4567")]
        public void RedactPersonalData_replaces_phone_numbers(string phoneNumber)
        {
            var before = $"Call {phoneNumber} to test";
            var after = "Call ***** ****** to test";
            var redactor = new DataRedactor();

            var result = redactor.RedactPersonalData(before);

            Assert.Equal(after, result);
        }
    }
}
