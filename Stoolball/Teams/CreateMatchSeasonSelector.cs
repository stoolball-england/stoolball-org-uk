using Stoolball.Competitions;
using Stoolball.Matches;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Stoolball.Teams
{
    public class CreateMatchSeasonSelector : ICreateMatchSeasonSelector
    {
        public IList<Season> SelectPossibleSeasons(IEnumerable<TeamInSeason> seasons, MatchType matchType)
        {
            return seasons.Where(x => x.Season.MatchTypes.Contains(matchType) && !x.WithdrawnDate.HasValue && x.Season.UntilYear >= DateTime.Now.Year).Select(x => x.Season).ToList();
        }
    }
}
