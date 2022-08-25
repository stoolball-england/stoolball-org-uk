using System;
using System.Threading.Tasks;
using Moq;
using Stoolball.Routing;
using Xunit;

namespace Stoolball.UnitTests.Routing
{
    public class RouteGeneratorTests
    {
        private readonly Mock<IRouteTokeniser> _tokeniser = new();

        [Fact]
        public void Route_is_lowercase()
        {
            var original = "MiXeD";
            _tokeniser.Setup(x => x.TokeniseRoute(original)).Returns((original, null));
            var generator = new RouteGenerator(_tokeniser.Object);

            var result = generator.GenerateRoute(string.Empty, original, Array.Empty<string>());

            Assert.Equal("/mixed", result);
        }

        [Fact]
        public void Punctuation_is_removed()
        {
            var original = "example? route's punctuation; good! example.";
            _tokeniser.Setup(x => x.TokeniseRoute(original)).Returns((original, null));
            var generator = new RouteGenerator(_tokeniser.Object);

            var result = generator.GenerateRoute(string.Empty, original, Array.Empty<string>());

            Assert.Equal("/example-routes-punctuation-good-example", result);
        }

        [Fact]
        public void Noise_word_removed_from_start()
        {
            var original = "stoolball-ladies";
            _tokeniser.Setup(x => x.TokeniseRoute(original)).Returns((original, null));
            var generator = new RouteGenerator(_tokeniser.Object);

            var result = generator.GenerateRoute(string.Empty, original, new[] { "stoolball" });

            Assert.Equal("/ladies", result);
        }

        [Fact]
        public void Noise_word_removed_from_middle()
        {
            var original = "some-stoolball-friends";
            _tokeniser.Setup(x => x.TokeniseRoute(original)).Returns((original, null));
            var generator = new RouteGenerator(_tokeniser.Object);

            var result = generator.GenerateRoute(string.Empty, original, new[] { "stoolball" });

            Assert.Equal("/some-friends", result);
        }

        [Fact]
        public void Noise_word_removed_from_end()
        {
            var original = "somewhere-club";
            _tokeniser.Setup(x => x.TokeniseRoute(original)).Returns((original, null));
            var generator = new RouteGenerator(_tokeniser.Object);

            var result = generator.GenerateRoute(string.Empty, original, new[] { "club" });

            Assert.Equal("/somewhere", result);
        }


        [Fact]
        public void Prefix_is_added()
        {
            var prefix = "prefix";
            var original = "example";
            _tokeniser.Setup(x => x.TokeniseRoute(original)).Returns((original, null));
            var generator = new RouteGenerator(_tokeniser.Object);

            var result = generator.GenerateRoute(prefix, original, Array.Empty<string>());

            Assert.Equal("prefix/example", result);
        }

        [Fact]
        public void Increment_adds_1_where_no_counter()
        {
            var original = "example";
            _tokeniser.Setup(x => x.TokeniseRoute(original)).Returns((original, null));
            var generator = new RouteGenerator(_tokeniser.Object);

            var result = generator.IncrementRoute(original);

            Assert.Equal("example-1", result);
        }

        [Fact]
        public void Increment_adds_1_to_existing_counter()
        {
            var original = "example-1";
            _tokeniser.Setup(x => x.TokeniseRoute(original)).Returns(("example", 1));
            var generator = new RouteGenerator(_tokeniser.Object);

            var result = generator.IncrementRoute(original);

            Assert.Equal("example-2", result);
        }

        [Fact]
        public void Increment_adds_1_to_existing_counter_greater_than_9()
        {
            var original = "example-10";
            _tokeniser.Setup(x => x.TokeniseRoute(original)).Returns(("example", 10));
            var generator = new RouteGenerator(_tokeniser.Object);

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
            _tokeniser.Setup(x => x.TokeniseRoute("/original")).Returns(("/original", null));
            _tokeniser.Setup(x => x.TokeniseRoute("/original-1")).Returns(("/original", 1));
            _tokeniser.Setup(x => x.TokeniseRoute("/original-2")).Returns(("/original", 2));
            _tokeniser.Setup(x => x.TokeniseRoute("/something-went-wrong")).Returns(("/something-went-wrong", null));
            var generator = new RouteGenerator(_tokeniser.Object);

            var result = generator.IsMatchingRoute(original, generated);

            Assert.Equal(shouldMatch, result);
        }

        [Fact]
        public async Task GenerateUniqueRoute_returns_route_with_no_counter_when_current_route_is_empty_string()
        {
            var generator = new RouteGenerator(_tokeniser.Object);

            var result = await generator.GenerateUniqueRoute(string.Empty, null, "Example thing", Array.Empty<string>(), x => Task.FromResult(0)).ConfigureAwait(false);

            Assert.Equal("/example-thing", result);
        }
    }
}
