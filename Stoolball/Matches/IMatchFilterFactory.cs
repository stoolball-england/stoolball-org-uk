using System;
using System.Collections.Generic;

namespace Stoolball.Matches
{
    public interface IMatchFilterFactory
    {
        (MatchFilter filter, MatchSortOrder sortOrder) MatchesForTeams(List<Guid> teamIds);
        (MatchFilter filter, MatchSortOrder sortOrder) MatchesForMatchLocation(Guid matchLocationId);
        (MatchFilter filter, MatchSortOrder sortOrder) MatchesForSeason(Guid seasonId);
        (MatchFilter filter, MatchSortOrder sortOrder) MatchesForTournament(Guid tournamentId);
    }
}