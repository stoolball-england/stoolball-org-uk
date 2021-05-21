using Stoolball.Html;
using Xunit;

namespace Stoolball.UnitTests.Html
{
    public class BadLanguageFilterTests
    {
        [Fact]
        public void Null_does_not_throw()
        {
            var filter = new BadLanguageFilter();

            var result = filter.Filter(null);

            Assert.Null(result);
        }

        [Fact]
        public void Empty_string_does_not_throw()
        {
            var filter = new BadLanguageFilter();

            var result = filter.Filter(string.Empty);

            Assert.Equal(string.Empty, result);
        }

        [Theory]
        [InlineData("This shit should get removed", "This @*%! should get removed")]
        [InlineData("This fuck should get removed", "This @*%! should get removed")]
        [InlineData("This FUCKING word should get removed", "This @*%! word should get removed")]
        [InlineData("This ShIt should get removed", "This @*%! should get removed")]
        public void Bad_language_is_replaced_case_insensitive(string text, string expected)
        {
            var filter = new BadLanguageFilter();

            var result = filter.Filter(text);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Good_language_is_unaltered()
        {
            var text = "This should be fine";
            var filter = new BadLanguageFilter();

            var result = filter.Filter(text);

            Assert.Equal(text, result);
        }
    }
}
