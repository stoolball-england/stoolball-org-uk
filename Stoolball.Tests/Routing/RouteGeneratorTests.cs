using Stoolball.Routing;
using Xunit;

namespace Stoolball.Tests.Routing
{
    public class RouteGeneratorTests
    {
        [Fact]
        public void Route_is_lowercase()
        {
            var original = "MiXeD";
            var generator = new RouteGenerator();

            var result = generator.GenerateRoute(string.Empty, original);

            Assert.Equal("mixed", result);
        }

        [Fact]
        public void Punctuation_is_removed()
        {
            var original = "example? route's punctuation; good! example.";
            var generator = new RouteGenerator();

            var result = generator.GenerateRoute(string.Empty, original);

            Assert.Equal("example-routes-punctuation-good-example", result);
        }

        [Fact]
        public void Noise_word_removed_from_start()
        {
            var original = "stoolball-ladies";
            var generator = new RouteGenerator();

            var result = generator.GenerateRoute(string.Empty, original);

            Assert.Equal("ladies", result);
        }

        [Fact]
        public void Noise_word_removed_from_middle()
        {
            var original = "some-stoolball-friends";
            var generator = new RouteGenerator();

            var result = generator.GenerateRoute(string.Empty, original);

            Assert.Equal("some-friends", result);
        }

        [Fact]
        public void Noise_word_removed_from_end()
        {
            var original = "somewhere-club";
            var generator = new RouteGenerator();

            var result = generator.GenerateRoute(string.Empty, original);

            Assert.Equal("somewhere", result);
        }


        [Fact]
        public void Prefix_is_added()
        {
            var prefix = "prefix";
            var original = "example";
            var generator = new RouteGenerator();

            var result = generator.GenerateRoute(prefix, original);

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
    }
}
