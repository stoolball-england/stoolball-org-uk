using System;
using System.Threading.Tasks;

namespace Stoolball.Matches
{
    public interface IMatchListingCacheClearer
    {
        Task ClearCacheForTournament(Tournament tournament);
        Task ClearCacheForTournament(Tournament tournamentBefore, Tournament tournamentAfter);
        Task ClearCacheForMatch(Match match);
        Task ClearCacheForMatch(Match matchBeforeUpdate, Match matchAfterUpdate);
        Task ClearCacheForTeam(Guid teamId);
        void ClearCacheForMatchLocation(Guid matchLocationId);
        void ClearCacheForSeason(Guid seasonId);
        void ClearCacheForTournamentMatches(Guid tournamentId);
    }
}