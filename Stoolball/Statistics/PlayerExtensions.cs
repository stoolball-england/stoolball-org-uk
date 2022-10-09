using System;
using System.Linq;
using Humanizer;

namespace Stoolball.Statistics
{
    public static class PlayerExtensions
    {
        public static string LimitedListOfTeams(this Player player)
        {
            if (player is null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            var teamNames = player.PlayerIdentities.Where(x => x.Team?.TeamName != null).OrderByDescending(x => x.TotalMatches).Select(x => x.Team!.TeamName).Distinct();
            if (teamNames.Count() > 3)
            {
                teamNames = teamNames.Take(3).Append("others");
            }
            return teamNames.Humanize();
        }
    }
}