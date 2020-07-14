using Stoolball.Competitions;
using Stoolball.Matches;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Stoolball.Teams
{
    public class CreateLeagueMatchEligibleSeasons : ICreateLeagueMatchEligibleSeasons
    {
        public IEnumerable<Season> SelectEligibleSeasons(IEnumerable<TeamInSeason> seasons)
        {
            return seasons.Where(x => x.Season.MatchTypes.Contains(MatchType.LeagueMatch) && !x.WithdrawnDate.HasValue && x.Season.UntilYear >= DateTime.Now.Year).Select(x => x.Season);
        }
    }
}
