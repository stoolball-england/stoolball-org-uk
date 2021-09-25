using Xunit;

namespace Stoolball.UnitTests
{
    public class SocialMediaAccountFormatterTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Null_or_empty_string_is_unaltered(string value)
        {
            var formatter = new SocialMediaAccountFormatter();

            var result = formatter.PrefixAtSign(value);

            Assert.Equal(value, result);
        }

        [Fact]
        public void Value_is_trimmed()
        {
            var formatter = new SocialMediaAccountFormatter();

            var result = formatter.PrefixAtSign("  @example  ");

            Assert.Equal("@example", result);
        }

        [Fact]
        public void Case_is_unaltered()
        {
            var formatter = new SocialMediaAccountFormatter();

            var result = formatter.PrefixAtSign("ExAmPlE");

            Assert.Equal("@ExAmPlE", result);
        }

        [Theory]
        [InlineData("test")]
        [InlineData("@test")]
        public void Prefix_is_added_only_if_missing(string value)
        {
            var formatter = new SocialMediaAccountFormatter();

            var result = formatter.PrefixAtSign(value);

            Assert.Equal("@test", result);
        }
    }
}
