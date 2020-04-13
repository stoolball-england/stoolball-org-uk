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
        /// <param name="subrouteRegex">(valid|alsoValid)</param>
        /// <returns>prefix/entity-route</returns>
        /// <exception cref="ArgumentException">If the <c>route</c> or the <c>expectedPrefix</c> is not in a valid format</exception>
        public string NormaliseRouteToEntity(string route, string expectedPrefix, string subrouteRegex)
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
            if (!normalisedRoute.StartsWith(normalisedPrefix + "/", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Route must start with '{normalisedPrefix}/. It was {route}.", nameof(route));
            }

            if (normalisedRoute.Length <= normalisedPrefix.Length + 1)
            {
                throw new ArgumentException($"Route must include a segment beyond the prefix. It was {route}.", nameof(route));
            }

            if (normalisedRoute.Substring(normalisedPrefix.Length + 1).Contains("/"))
            {
                // If there's anything after prefix/route, test it against the Regex or either discard it 
                if (!string.IsNullOrWhiteSpace(subrouteRegex))
                {
                    var subroute = normalisedRoute.Substring(normalisedRoute.Substring(normalisedPrefix.Length + 1).IndexOf("/", StringComparison.OrdinalIgnoreCase) + normalisedPrefix.Length + 2);
                    if (!Regex.IsMatch(subroute, subrouteRegex))
                    {
                        throw new ArgumentException($"Sub-route must match the regular expression '{subrouteRegex}'. Sub-route {subroute} was identified in the route {route}.", nameof(route));
                    }
                }
                else
                {
                    normalisedRoute = normalisedRoute.Substring(0, normalisedRoute.Substring(normalisedPrefix.Length + 1).IndexOf("/", StringComparison.OrdinalIgnoreCase) + normalisedPrefix.Length + 1);
                }
            }

            return "/" + normalisedRoute;
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