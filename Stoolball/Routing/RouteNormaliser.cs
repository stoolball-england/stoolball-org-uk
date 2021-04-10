using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Stoolball.Routing
{
    /// <summary>
    /// Parses, validates and tidies up route values into expected formats
    /// </summary>
    public class RouteNormaliser : IRouteNormaliser
    {
        /// <summary>
        /// From a given route, removes any extraneous / characters and returns only the portion identifying an entity, ie prefix/entity-route
        /// </summary>
        /// <param name="route">prefix/entity-route</param>
        /// <param name="expectedPrefixAndRegex">Dictionary where the key is the expected prefix and the value is the valid entity route regex</param>
        /// <returns>prefix/entity-route</returns>
        /// <exception cref="ArgumentException">If the <c>route</c> or the <c>expectedPrefix</c> is not in a valid format</exception>
        public string NormaliseRouteToEntity(string route, Dictionary<string, string> expectedPrefixAndRegex)
        {
            if (string.IsNullOrWhiteSpace(route) && route != "/")
            {
                throw new ArgumentException($"{nameof(route)} must not be blank or /", nameof(route));
            }

            if (expectedPrefixAndRegex is null)
            {
                throw new ArgumentNullException(nameof(expectedPrefixAndRegex));
            }

            ArgumentException exception = null;
            foreach (var expectedPrefix in expectedPrefixAndRegex.Keys)
            {
                var entityRouteRegex = string.IsNullOrEmpty(expectedPrefixAndRegex[expectedPrefix]) ? "^[a-z0-9-]+$" : expectedPrefixAndRegex[expectedPrefix];

                if (string.IsNullOrWhiteSpace(expectedPrefix) && expectedPrefix != "/")
                {
                    exception = new ArgumentException($"Expected prefix must not be blank or /", nameof(expectedPrefixAndRegex));
                    continue;
                }

                var normalisedRoute = route.Trim('/').ToLower(CultureInfo.CurrentCulture);
                var normalisedPrefix = expectedPrefix.Trim('/').ToLower(CultureInfo.CurrentCulture);
                var splitRoute = normalisedRoute.Split('/');

                if (splitRoute.Length == 0 || splitRoute[0] != normalisedPrefix)
                {
                    exception = new ArgumentException($"Route must start with '{normalisedPrefix}/. It was {route}.", nameof(route));
                    continue;
                }

                if (splitRoute.Length == 1)
                {
                    exception = new ArgumentException($"Route must include an entity route beyond the prefix. It was {route}.", nameof(route));
                    continue;
                }


                var entityRoute = string.Empty;
                for (var i = 1; i < splitRoute.Length; i++)
                {
                    if (entityRoute.Length > 0) { entityRoute += "/"; }
                    entityRoute += splitRoute[i];
                    entityRoute = Path.ChangeExtension(entityRoute, string.Empty).TrimEnd('.');

                    if (Regex.IsMatch(entityRoute, entityRouteRegex))
                    {
                        return $"/{normalisedPrefix}/{entityRoute}";
                    }
                }

                exception = new ArgumentException($"Entity route must match the regular expression '{entityRouteRegex}'. Entity route {entityRoute} was identified in the route {route}.", nameof(route));
            }

            if (exception == null)
            {
                exception = new ArgumentException($"Entity route in the route {route} must match one of the provided regular expressions.", nameof(route));
            }
            throw exception;
        }

        /// <summary>
        /// From a given route, removes any extraneous / characters and returns only the portion identifying an entity, ie prefix/entity-route
        /// </summary>
        /// <param name="route">prefix/entity-route</param>
        /// <param name="expectedPrefix">prefix</param>
        /// <param name="entityRouteRegex">(valid|alsoValid)</param>
        /// <returns>prefix/entity-route</returns>
        public string NormaliseRouteToEntity(string route, string expectedPrefix, string entityRouteRegex)
        {
            return NormaliseRouteToEntity(route, new Dictionary<string, string> { { expectedPrefix, entityRouteRegex } });
        }

        /// <summary>
        /// From a given route, removes any extraneous / characters and returns only the portion identifying an entity, ie prefix/entity-route
        /// </summary>
        /// <param name="route">prefix/entity-route</param>
        /// <param name="expectedPrefix">prefix</param>
        /// <returns>prefix/entity-route</returns>
        /// <exception cref="ArgumentException">If the <c>route</c> or the <c>expectedPrefix</c> is not in a valid format</exception>
        public string NormaliseRouteToEntity(string route, string expectedPrefix)
        {
            return NormaliseRouteToEntity(route, expectedPrefix, null);
        }
    }
}