using Humanizer;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Stoolball.Routing
{
    public class RouteGenerator : IRouteGenerator
    {
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

        public string IncrementRoute(string route)
        {
            if (string.IsNullOrEmpty(route))
            {
                throw new System.ArgumentException("message", nameof(route));
            }

            var match = Regex.Match(route, "-(?<counter>[0-9]+)$");
            if (match.Success)
            {
                var counter = int.Parse(match.Groups["counter"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                return $"{route.Substring(0, match.Index)}-{++counter}";
            }
            else
            {
                return route + "-1";
            }
        }
    }
}
