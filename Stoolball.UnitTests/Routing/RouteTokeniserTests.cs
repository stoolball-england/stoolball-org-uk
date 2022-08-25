using Stoolball.Routing;
using Xunit;

namespace Stoolball.UnitTests.Routing
{
    public class RouteTokeniserTests
    {
        [Fact]
        public void Returns_null_if_no_counter()
        {
            var original = "/example-route";
            var tokeniser = new RouteTokeniser();

            var result = tokeniser.TokeniseRoute(original);

            Assert.Equal(original, result.baseRoute);
            Assert.Null(result.counter);
        }

        [Theory]
        [InlineData("1", 1)]
        [InlineData("1234", 1234)]
        public void Returns_parsed_counter(string counter, int expected)
        {
            var original = $"/example-route-{counter}";
            var tokeniser = new RouteTokeniser();

            var result = tokeniser.TokeniseRoute(original);

            Assert.Equal("/example-route", result.baseRoute);
            Assert.Equal(expected, result.counter);
        }
    }
}
