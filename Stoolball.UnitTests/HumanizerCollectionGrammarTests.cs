using Xunit;

namespace Stoolball.UnitTests
{
    public class HumanizerCollectionGrammarTests
    {
        [Fact]
        public void Single_item_returns_item()
        {
            var grammar = new HumanizerCollectionGrammar();

            var result = grammar.Humanize(new[] { "example" });

            Assert.Equal("example", result);
        }

        [Fact]
        public void Two_items_uses_and()
        {
            var grammar = new HumanizerCollectionGrammar();

            var result = grammar.Humanize(new[] { "example", "example" });

            Assert.Equal("example and example", result);
        }

        [Fact]
        public void Three_items_uses_comma_then_and()
        {
            var grammar = new HumanizerCollectionGrammar();

            var result = grammar.Humanize(new[] { "example", "example", "example" });

            Assert.Equal("example, example and example", result);
        }

        [Fact]
        public void Final_item_separator_is_configurable()
        {
            var grammar = new HumanizerCollectionGrammar();

            var result = grammar.Humanize(new[] { "example", "example" }, "or");

            Assert.Equal("example or example", result);
        }
    }
}
