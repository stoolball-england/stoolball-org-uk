using System.Globalization;
using System.Text.RegularExpressions;

namespace Stoolball.Routing
{
    public class RouteTokeniser : IRouteTokeniser
    {
        public (string baseRoute, int? counter) TokeniseRoute(string route)
        {
            var match = Regex.Match(route, "-(?<counter>[0-9]+)$");
            if (match.Success)
            {
                var counter = int.Parse(match.Groups["counter"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                return (route.Substring(0, match.Index), counter);
            }
            else
            {
                return (route, null);
            }
        }
    }
}
