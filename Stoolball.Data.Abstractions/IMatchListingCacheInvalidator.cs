using System;
using System.Threading.Tasks;
using Stoolball.Matches;

namespace Stoolball.Data.Abstractions
{
    public interface IMatchListingCacheInvalidator
    {
        Task InvalidateCacheForTournament(Tournament tournament);
        Task InvalidateCacheForTournament(Tournament tournamentBefore, Tournament tournamentAfter);
        Task InvalidateCacheForMatch(Match match);
        Task InvalidateCacheForMatch(Match matchBeforeUpdate, Match matchAfterUpdate);
        Task InvalidateCacheForTeam(Guid teamId);
        void InvalidateCacheForMatchLocation(Guid matchLocationId);
        void InvalidateCacheForSeason(Guid seasonId);
        void InvalidateCacheForTournamentMatches(Guid tournamentId);
    }
}