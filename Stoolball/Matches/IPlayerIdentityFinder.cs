using System.Collections.Generic;
using Stoolball.Teams;

namespace Stoolball.Matches
{
    public interface IPlayerIdentityFinder
    {
        IEnumerable<PlayerIdentity> PlayerIdentitiesInMatch(Match match);
    }
}