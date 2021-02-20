using System.Collections.Generic;
using Stoolball.Matches;

namespace Stoolball.Statistics
{
    public interface IPlayerIdentityFinder
    {
        IEnumerable<PlayerIdentity> PlayerIdentitiesInMatch(Match match);
    }
}