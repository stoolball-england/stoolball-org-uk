using Stoolball.Competitions;
using Stoolball.Matches;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Stoolball.Teams
{
    public class CreateLeagueMatchSeasonSelector : ICreateLeagueMatchSeasonSelector
    {
        public IEnumerable<Season> SelectPossibleSeasons(IEnumerable<TeamInSeason> seasons)
        {
            return seasons.Where(x => x.Season.MatchTypes.Contains(MatchType.LeagueMatch) && !x.WithdrawnDate.HasValue && x.Season.UntilYear >= DateTime.Now.Year).Select(x => x.Season);
        }
    }
}
