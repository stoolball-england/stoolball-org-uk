﻿using System;
using System.Collections.Generic;
using Stoolball.Routing;
using Xunit;

namespace Stoolball.UnitTests.Routing
{
    public class RouteNormaliserTests
    {
        [Theory]
        [InlineData("prefix/route-value", "prefix")]
        [InlineData("/prefix/route-value/", "prefix")]
        [InlineData("/prefix/route-value/sub-route", "prefix")]
        [InlineData("/prefix/route-value.extension", "prefix")]
        [InlineData("/PREFIX/ROUTE-VALUE", "prefix")]
        [InlineData("prefix/route-value", "/prefix/")]
        public void Route_should_normalise_to_prefix_routevalue(string route, string expectedPrefix)
        {
            var normaliser = new RouteNormaliser();

            var result = normaliser.NormaliseRouteToEntity(route, expectedPrefix);

            Assert.Equal("/prefix/route-value", result);
        }

        [Theory]
        [InlineData("prefix/route-value/1234", "/prefix/", "/prefix/route-value/1234")]
        [InlineData("/prefix/route-value/1234", "/prefix/", "/prefix/route-value/1234")]
        [InlineData("/PREFIX/ROUTE-VALUE/1234/", "prefix", "/prefix/route-value/1234")]
        [InlineData("/prefix/route-value/1234-56/", "/prefix/", "/prefix/route-value/1234-56")]
        [InlineData("/prefix/route-value/1234/matches", "/prefix/", "/prefix/route-value/1234")]
        [InlineData("/prefix/route-value/1234-56/matches/", "/prefix/", "/prefix/route-value/1234-56")]
        public void Season_route_should_normalise_to_prefix_routevalue(string route, string expectedPrefix, string expectedResult)
        {
            var normaliser = new RouteNormaliser();

            var result = normaliser.NormaliseRouteToEntity(route, expectedPrefix, @"^[a-z0-9-]+\/[0-9]{4}(-[0-9]{2})?$");

            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void Second_route_should_normalise_to_prefix_routevalue()
        {
            var normaliser = new RouteNormaliser();

            var result = normaliser.NormaliseRouteToEntity("/tournaments/example123/teams/example-team/",
                new Dictionary<string, string?> {
                    { "dont-match-this-or-throw", null },
                    {"tournaments", @"^[a-z0-9-]+\/teams\/[a-z0-9-]+$" }
                });

            Assert.Equal("/tournaments/example123/teams/example-team", result);
        }

        [Fact]
        public void Null_prefix_should_throw_ArgumentNullException()
        {
            var normaliser = new RouteNormaliser();

            string? expectedPrefix = null;
            Assert.Throws<ArgumentNullException>(() => normaliser.NormaliseRouteToEntity("prefix/route-value", expectedPrefix));
        }

        [Theory]
        [InlineData(null, "prefix")]
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
        public void Invalid_subRoute_should_throw_ArgumentException(string route, string expectedPrefix, string entityRouteRegex)
        {
            var normaliser = new RouteNormaliser();

            Assert.Throws<ArgumentException>(() => normaliser.NormaliseRouteToEntity(route, expectedPrefix, entityRouteRegex));
        }
    }
}
