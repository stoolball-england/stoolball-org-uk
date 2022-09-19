using System;
using System.Collections.Generic;

namespace Stoolball.Matches
{
    public interface IMatchFinder
    {
        IEnumerable<Match> MatchesPlayedByPlayerIdentity(IEnumerable<Match> matches, Guid playerIdentityId);
    }
}