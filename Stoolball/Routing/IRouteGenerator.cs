using System.Collections.Generic;

namespace Stoolball.Routing
{
    public interface IRouteGenerator
    {
        string GenerateRoute(string prefix, string name, IEnumerable<string> noiseWords);
        string IncrementRoute(string route);

        /// <summary>
        /// Determines whether <c>routeAfter</c> is the same as <c>routeBefore</c> or incremented from it.
        /// </summary>
        bool IsMatchingRoute(string routeBefore, string routeAfter);
    }
}