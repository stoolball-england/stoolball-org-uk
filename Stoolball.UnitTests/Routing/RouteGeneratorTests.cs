using System;
using System.Threading.Tasks;
using Stoolball.Routing;
using Xunit;

namespace Stoolball.UnitTests.Routing
{
    public class RouteGeneratorTests
    {
        [Fact]
        public void Route_is_lowercase()
        {
            var original = "MiXeD";
            var generator = new RouteGenerator();

            var result = generator.GenerateRoute(string.Empty, original, Array.Empty<string>());

            Assert.Equal("/mixed", result);
        }

        [Fact]
        public void Punctuation_is_removed()
        {
            var original = "example? route's punctuation; good! example.";
            var generator = new RouteGenerator();

            var result = generator.GenerateRoute(string.Empty, original, Array.Empty<string>());

            Assert.Equal("/example-routes-punctuation-good-example", result);
        }

        [Fact]
        public void Noise_word_removed_from_start()
        {
            var original = "stoolball-ladies";
            var generator = new RouteGenerator();

            var result = generator.GenerateRoute(string.Empty, original, new[] { "stoolball" });

            Assert.Equal("/ladies", result);
        }

        [Fact]
        public void Noise_word_removed_from_middle()
        {
            var original = "some-stoolball-friends";
            var generator = new RouteGenerator();

            var result = generator.GenerateRoute(string.Empty, original, new[] { "stoolball" });

            Assert.Equal("/some-friends", result);
        }

        [Fact]
        public void Noise_word_removed_from_end()
        {
            var original = "somewhere-club";
            var generator = new RouteGenerator();

            var result = generator.GenerateRoute(string.Empty, original, new[] { "club" });

            Assert.Equal("/somewhere", result);
        }


        [Fact]
        public void Prefix_is_added()
        {
            var prefix = "prefix";
            var original = "example";
            var generator = new RouteGenerator();

            var result = generator.GenerateRoute(prefix, original, Array.Empty<string>());

            Assert.Equal("prefix/example", result);
        }

        [Fact]
        public void Increment_adds_1_where_no_counter()
        {
            var original = "example";
            var generator = new RouteGenerator();

            var result = generator.IncrementRoute(original);

            Assert.Equal("example-1", result);
        }

        [Fact]
        public void Increment_adds_1_to_existing_counter()
        {
            var original = "example-1";
            var generator = new RouteGenerator();

            var result = generator.IncrementRoute(original);

            Assert.Equal("example-2", result);
        }

        [Fact]
        public void Increment_adds_1_to_existing_counter_greater_than_9()
        {
            var original = "example-10";
            var generator = new RouteGenerator();

            var result = generator.IncrementRoute(original);

            Assert.Equal("example-11", result);
        }

        [Theory]
        [InlineData("/original", "/original", true)]
        [InlineData("/original", "/original-1", true)]
        [InlineData("/original-1", "/original-2", true)]
        [InlineData("/original", "/something-went-wrong", false)]
        public void Routes_should_match_disregarding_counter(string original, string generated, bool shouldMatch)
        {
            var generator = new RouteGenerator();

            var result = generator.IsMatchingRoute(original, generated);

            Assert.Equal(shouldMatch, result);
        }

        [Fact]
        public async Task GenerateUniqueRoute_returns_route_with_no_counter_when_current_route_is_empty_string()
        {
            var generator = new RouteGenerator();

            var result = await generator.GenerateUniqueRoute(string.Empty, null, "Example thing", Array.Empty<string>(), x => Task.FromResult(0)).ConfigureAwait(false);

            Assert.Equal("/example-thing", result);
        }
    }
}
