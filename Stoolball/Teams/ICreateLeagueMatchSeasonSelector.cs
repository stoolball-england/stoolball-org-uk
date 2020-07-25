using Stoolball.Competitions;
using System.Collections.Generic;

namespace Stoolball.Teams
{
    public interface ICreateLeagueMatchSeasonSelector
    {
        IEnumerable<Season> SelectPossibleSeasons(IEnumerable<TeamInSeason> seasons);
    }
}