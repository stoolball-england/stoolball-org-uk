using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Humanizer;

namespace Stoolball.Routing
{
    public class RouteGenerator : IRouteGenerator
    {
        public async Task<string> GenerateUniqueRoute(string prefix, string name, IEnumerable<string> noiseWords, Func<string, Task<int>> findMatchingRoutes)
        {
            return await GenerateUniqueRoute(string.Empty, prefix, name, noiseWords, findMatchingRoutes).ConfigureAwait(false);
        }

        public async Task<string> GenerateUniqueRoute(string currentRoute, string prefix, string name, IEnumerable<string> noiseWords, Func<string, Task<int>> findMatchingRoutes)
        {
            if (findMatchingRoutes is null)
            {
                throw new ArgumentNullException(nameof(findMatchingRoutes));
            }

            var baseRoute = GenerateRoute(prefix, name, noiseWords);
            var uniqueRoute = currentRoute;
            if (!IsMatchingRoute(currentRoute, baseRoute))
            {
                uniqueRoute = baseRoute;
                int count;
                do
                {
                    count = await findMatchingRoutes(uniqueRoute).ConfigureAwait(false);
                    if (count > 0)
                    {
                        uniqueRoute = IncrementRoute(uniqueRoute);
                    }
                }
                while (count > 0);
            }

            return uniqueRoute;
        }

        /// <inheritdoc/>
        public string GenerateRoute(string prefix, string name, IEnumerable<string> noiseWords)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new System.ArgumentException("message", nameof(name));
            }

            if (noiseWords is null)
            {
                throw new System.ArgumentNullException(nameof(noiseWords));
            }

            var route = Regex.Replace(name.ToLower(CultureInfo.CurrentCulture).Kebaberize(), "[^a-z0-9-]", string.Empty);
            foreach (var noiseWord in noiseWords)
            {
                route = Regex.Replace(route, $@"\b{noiseWord}\b", string.Empty);
            }
            route = Regex.Replace(route, "-+", "-").Trim('-');
            return string.IsNullOrEmpty(prefix) ? route : $"{prefix}/{route}";
        }

        /// <inheritdoc/>
        public string IncrementRoute(string route)
        {
            if (string.IsNullOrEmpty(route))
            {
                throw new System.ArgumentException($"'{nameof(route)}' cannot be null or empty", nameof(route));
            }

            var (baseRoute, counter) = SplitRouteAndCounter(route);
            if (counter != null)
            {
                var updatedCounter = int.Parse(counter, NumberStyles.Integer, CultureInfo.InvariantCulture);
                return $"{baseRoute}-{++updatedCounter}";
            }
            else
            {
                return route + "-1";
            }
        }

        private static (string baseRoute, string counter) SplitRouteAndCounter(string route)
        {
            var match = Regex.Match(route, "-(?<counter>[0-9]+)$");
            if (match.Success)
            {
                return (route.Substring(0, match.Index), match.Groups["counter"].Value);
            }
            else
            {
                return (route, null);
            }
        }

        /// <inheritdoc/>
        public bool IsMatchingRoute(string routeBefore, string routeAfter)
        {
            if (string.IsNullOrEmpty(routeBefore))
            {
                throw new System.ArgumentException($"'{nameof(routeBefore)}' cannot be null or empty", nameof(routeBefore));
            }

            if (string.IsNullOrEmpty(routeAfter))
            {
                throw new System.ArgumentException($"'{nameof(routeAfter)}' cannot be null or empty", nameof(routeAfter));
            }

            var (baseRouteBefore, _) = SplitRouteAndCounter(routeBefore);
            var (baseRouteAfter, _) = SplitRouteAndCounter(routeAfter);
            return (baseRouteAfter == baseRouteBefore);
        }
    }
}
