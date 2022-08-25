using Moq;
using Stoolball.Routing;
using Xunit;

namespace Stoolball.UnitTests.Routing
{
    public class BestRouteSelectorTests
    {
        private readonly Mock<IRouteTokeniser> _tokeniser = new();

        [Fact]
        public void Route_with_counter_and_route_without_should_choose_without()
        {
            var route1 = "/example-route";
            var route2 = "/example-route-111";
            _tokeniser.Setup(x => x.TokeniseRoute(route1)).Returns(("/example-route", null));
            _tokeniser.Setup(x => x.TokeniseRoute(route2)).Returns(("/example-route", 111));
            var selector = new BestRouteSelector(_tokeniser.Object);

            var result = selector.SelectBestRoute(route1, route2);

            Assert.Equal(route1, result);
        }

        [Fact]
        public void Routes_with_equal_counter_should_choose_longest()
        {
            var route1 = "/example-route-2";
            var route2 = "/example-routes-2";
            _tokeniser.Setup(x => x.TokeniseRoute(route1)).Returns(("/example-route", 2));
            _tokeniser.Setup(x => x.TokeniseRoute(route2)).Returns(("/example-route", 2));
            var selector = new BestRouteSelector(_tokeniser.Object);

            var result = selector.SelectBestRoute(route1, route2);

            Assert.Equal(route2, result);
        }

        [Fact]
        public void Routes_with_unequal_counter_should_choose_lower()
        {
            var route1 = "/example-route-2";
            var route2 = "/example-route-11";
            _tokeniser.Setup(x => x.TokeniseRoute(route1)).Returns(("/example-route", 2));
            _tokeniser.Setup(x => x.TokeniseRoute(route2)).Returns(("/example-route", 11));
            var selector = new BestRouteSelector(_tokeniser.Object);

            var result = selector.SelectBestRoute(route1, route2);

            Assert.Equal(route1, result);
        }

        [Fact]
        public void Both_routes_without_counter_should_choose_longest()
        {
            var route1 = "/example-route";
            var route2 = "/example-routes";
            _tokeniser.Setup(x => x.TokeniseRoute(route1)).Returns(("/example-route", null));
            _tokeniser.Setup(x => x.TokeniseRoute(route2)).Returns(("/example-route", null));
            var selector = new BestRouteSelector(_tokeniser.Object);

            var result = selector.SelectBestRoute(route1, route2);

            Assert.Equal(route2, result);
        }
    }
}
