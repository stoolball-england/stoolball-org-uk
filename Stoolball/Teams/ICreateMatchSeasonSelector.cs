using Stoolball.Competitions;
using Stoolball.Matches;
using System.Collections.Generic;

namespace Stoolball.Teams
{
    public interface ICreateMatchSeasonSelector
    {
        IList<Season> SelectPossibleSeasons(IEnumerable<TeamInSeason> seasons, MatchType matchType);
    }
}