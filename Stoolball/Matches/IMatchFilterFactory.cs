using System;

namespace Stoolball.Matches
{
    public interface IMatchFilterFactory
    {
        (MatchFilter filter, MatchSortOrder sortOrder) MatchesForTeam(Guid teamId);
        (MatchFilter filter, MatchSortOrder sortOrder) MatchesForMatchLocation(Guid matchLocationId);
        (MatchFilter filter, MatchSortOrder sortOrder) MatchesForSeason(Guid seasonId);
    }
}