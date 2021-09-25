using System;
using System.Text.RegularExpressions;

namespace Stoolball
{
    public class StoolballEntityRouteParser : IStoolballEntityRouteParser
    {
        public StoolballEntityType? ParseRoute(string route)
        {
            if (route is null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            if (route.StartsWith("/players/", StringComparison.OrdinalIgnoreCase))
            {
                return StoolballEntityType.Player;
            }
            else if (route.StartsWith("/clubs/", StringComparison.OrdinalIgnoreCase))
            {
                return StoolballEntityType.Club;
            }
            else if (route.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                return StoolballEntityType.Team;
            }
            else if (route.StartsWith("/locations/", StringComparison.OrdinalIgnoreCase))
            {
                return StoolballEntityType.MatchLocation;
            }
            else if (Regex.IsMatch(route, @"\/competitions\/" + Constants.Pages.SeasonUrlRegEx.TrimStart('^').TrimEnd('$'), RegexOptions.IgnoreCase))
            {
                return StoolballEntityType.Season;
            }
            else if (route.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                return StoolballEntityType.Competition;
            }
            return null;
        }
    }
}