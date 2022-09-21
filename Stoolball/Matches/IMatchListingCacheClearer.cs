using System;
using System.Threading.Tasks;

namespace Stoolball.Matches
{
    public interface IMatchListingCacheClearer
    {
        Task ClearCacheFor(Tournament tournament);
        Task ClearCacheFor(Tournament tournamentBefore, Tournament tournamentAfter);
        Task ClearCacheFor(Match match);
        Task ClearCacheFor(Match matchBeforeUpdate, Match matchAfterUpdate);
        Task ClearCacheForTeam(Guid teamId);
    }
}