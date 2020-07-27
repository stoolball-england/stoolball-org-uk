using Stoolball.Competitions;
using Stoolball.Matches;
using System.Collections.Generic;

namespace Stoolball.Teams
{
    public interface ICreateMatchSeasonSelector
    {
        IEnumerable<Season> SelectPossibleSeasons(IEnumerable<TeamInSeason> seasons, MatchType matchType);
    }
}