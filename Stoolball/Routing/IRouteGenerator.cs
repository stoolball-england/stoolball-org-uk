using System.Collections.Generic;

namespace Stoolball.Routing
{
    public interface IRouteGenerator
    {
        string GenerateRoute(string prefix, string name, IEnumerable<string> noiseWords);
        string IncrementRoute(string route);
    }
}