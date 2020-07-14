using Stoolball.Competitions;
using System.Collections.Generic;

namespace Stoolball.Teams
{
    public interface ICreateLeagueMatchEligibleSeasons
    {
        IEnumerable<Season> SelectEligibleSeasons(IEnumerable<TeamInSeason> seasons);
    }
}