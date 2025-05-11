using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stoolball.Routing
{
    public interface IRouteGenerator
    {
        Task<string> GenerateUniqueRoute(string prefix, string name, IEnumerable<string> noiseWords, Func<string, Task<int>> findMatchingRoutes);
        Task<string> GenerateUniqueRoute(string currentRoute, string prefix, string name, IEnumerable<string> noiseWords, Func<string, Task<int>> findMatchingRoutes);

        string GenerateRoute(string prefix, string name, IEnumerable<string> noiseWords);

        /// <summary>
        /// Adds or increments a numeric suffix at the end of a route.
        /// </summary>
        /// <param name="route">The route which requires a numeric suffix added or incremented.</param>
        /// <returns>The amended route.</returns>
        string IncrementRoute(string route);

        /// <summary>
        /// Determines whether <c>routeAfter</c> is the same as <c>routeBefore</c> or incremented from it.
        /// </summary>
        bool IsMatchingRoute(string routeBefore, string routeAfter);
    }
}