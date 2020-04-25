using System;
using System.Globalization;
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
        /// <param name="expectedPrefix">prefix</param>
        /// <param name="entityRouteRegex">(valid|alsoValid)</param>
        /// <returns>prefix/entity-route</returns>
        /// <exception cref="ArgumentException">If the <c>route</c> or the <c>expectedPrefix</c> is not in a valid format</exception>
        public string NormaliseRouteToEntity(string route, string expectedPrefix, string entityRouteRegex)
        {
            if (string.IsNullOrWhiteSpace(route) && route != "/")
            {
                throw new ArgumentException($"{nameof(route)} must not be blank or /", nameof(route));
            }

            if (string.IsNullOrWhiteSpace(expectedPrefix) && expectedPrefix != "/")
            {
                throw new ArgumentException($"{nameof(expectedPrefix)} must not be blank or /", nameof(expectedPrefix));
            }

            var normalisedRoute = route.Trim('/').ToLower(CultureInfo.CurrentCulture);
            var normalisedPrefix = expectedPrefix.Trim('/').ToLower(CultureInfo.CurrentCulture);
            var splitRoute = normalisedRoute.Split('/');

            if (splitRoute.Length == 0 || splitRoute[0] != normalisedPrefix)
            {
                throw new ArgumentException($"Route must start with '{normalisedPrefix}/. It was {route}.", nameof(route));
            }

            if (splitRoute.Length == 1)
            {
                throw new ArgumentException($"Route must include an entity route beyond the prefix. It was {route}.", nameof(route));
            }


            var entityRoute = string.Empty;
            for (var i = 1; i < splitRoute.Length; i++)
            {
                if (entityRoute.Length > 0) { entityRoute += "/"; }
                entityRoute += splitRoute[i];

                if (Regex.IsMatch(entityRoute, entityRouteRegex))
                {
                    return $"/{normalisedPrefix}/{entityRoute}";
                }
            }

            throw new ArgumentException($"Entity route must match the regular expression '{entityRouteRegex}'. Entity route {entityRoute} was identified in the route {route}.", nameof(route));
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
            return NormaliseRouteToEntity(route, expectedPrefix, "^[a-z0-9-]+$");
        }
    }
}