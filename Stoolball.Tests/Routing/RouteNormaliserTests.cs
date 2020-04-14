using Stoolball.Routing;
using System;
using Xunit;

namespace Stoolball.Tests.Routing
{
    public class RouteNormaliserTests
    {
        [Theory]
        [InlineData("prefix/route-value", "prefix")]
        [InlineData("/prefix/route-value/", "prefix")]
        [InlineData("/prefix/route-value/sub-route", "prefix")]
        [InlineData("/PREFIX/ROUTE-VALUE", "prefix")]
        [InlineData("prefix/route-value", "/prefix/")]
        public void Route_should_normalise_to_prefix_routevalue(string route, string expectedPrefix)
        {
            var normaliser = new RouteNormaliser();

            var result = normaliser.NormaliseRouteToEntity(route, expectedPrefix);

            Assert.Equal("/prefix/route-value", result);
        }

        [Theory]
        [InlineData("prefix/route-value/1234", "prefix", "[0-9]{4}")]
        [InlineData("/prefix/route-value/1234/", "prefix", "[0-9]{4}")]
        [InlineData("/PREFIX/ROUTE-VALUE/1234", "prefix", "[0-9]{4}")]
        [InlineData("prefix/route-value/1234", "/prefix/", "[0-9]{4}")]
        public void Route_should_normalise_to_prefix_routevalue_subroute(string route, string expectedPrefix, string subrouteRegex)
        {
            var normaliser = new RouteNormaliser();

            var result = normaliser.NormaliseRouteToEntity(route, expectedPrefix, subrouteRegex);

            Assert.Equal("/prefix/route-value/1234", result);
        }

        [Theory]
        [InlineData(null, "prefix")]
        [InlineData("prefix/route-value", null)]
        [InlineData(" ", "prefix")]
        [InlineData("prefix/route-value", " ")]
        [InlineData("wrong/route-value", "prefix")]
        [InlineData("prefix/route-value", "wrong")]
        [InlineData(@"prefix\route-value", "prefix")]
        [InlineData("prefix", "prefix")]
        [InlineData("prefix/", "prefix")]
        [InlineData("prefix", "prefix/route-value")]
        public void Invalid_route_should_throw_ArgumentException(string route, string expectedPrefix)
        {
            var normaliser = new RouteNormaliser();

            Assert.Throws<ArgumentException>(() => normaliser.NormaliseRouteToEntity(route, expectedPrefix));
        }

        [Theory]
        [InlineData("prefix", "prefix/route-value/invalid", "(valid|alsoValid)")]
        public void Invalid_subRoute_should_throw_ArgumentException(string route, string expectedPrefix, string subrouteRegex)
        {
            var normaliser = new RouteNormaliser();

            Assert.Throws<ArgumentException>(() => normaliser.NormaliseRouteToEntity(route, expectedPrefix, subrouteRegex));
        }
    }
}
