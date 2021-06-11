using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Stoolball.Caching;
using Stoolball.Clubs;
using Stoolball.Matches;

namespace Stoolball.Data.Cache
{
    public class CacheClearingTournamentRepository : ITournamentRepository
    {
        private readonly IWrappableTournamentRepository _tournamentRepository;
        private readonly IClubDataSource _clubDataSource;
        private readonly IMatchFilterFactory _matchFilterFactory;
        private readonly IMatchFilterSerializer _matchFilterSerializer;
        private readonly IMemoryCache _memoryCache;

        public CacheClearingTournamentRepository(IWrappableTournamentRepository tournamentRepository, IClubDataSource clubDataSource,
            IMatchFilterFactory matchFilterFactory, IMatchFilterSerializer matchFilterSerializer, IMemoryCache memoryCache)
        {
            _tournamentRepository = tournamentRepository ?? throw new ArgumentNullException(nameof(tournamentRepository));
            _clubDataSource = clubDataSource ?? throw new ArgumentNullException(nameof(clubDataSource));
            _matchFilterFactory = matchFilterFactory ?? throw new ArgumentNullException(nameof(matchFilterFactory));
            _matchFilterSerializer = matchFilterSerializer ?? throw new ArgumentNullException(nameof(matchFilterSerializer));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        private async Task ClearImportantCachesForTournament(Tournament tournament)
        {
            var affectedTeams = tournament.Teams.Select(t => t.Team.TeamId.Value).ToList();
            foreach (var teamId in affectedTeams)
            {
                var filter = _matchFilterFactory.MatchesForTeams(new List<Guid> { teamId });
                var cacheKey = CacheConstants.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
                _memoryCache.Remove(cacheKey);
            }

            if (affectedTeams.Any())
            {
                var clubs = await _clubDataSource.ReadClubs(new ClubFilter { TeamIds = affectedTeams }).ConfigureAwait(false);
                foreach (var club in clubs)
                {
                    var filter = _matchFilterFactory.MatchesForTeams(club.Teams.Select(x => x.TeamId.Value).ToList());
                    var cacheKey = CacheConstants.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
                    _memoryCache.Remove(cacheKey);
                }
            }

            if (tournament?.TournamentLocation != null)
            {
                var filter = _matchFilterFactory.MatchesForMatchLocation(tournament.TournamentLocation.MatchLocationId.Value);
                var cacheKey = CacheConstants.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
                _memoryCache.Remove(cacheKey);
            }

            if (tournament != null && tournament.Seasons != null)
            {
                foreach (var season in tournament.Seasons)
                {
                    var filter = _matchFilterFactory.MatchesForSeason(season.SeasonId.Value);
                    var cacheKey = CacheConstants.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
                    _memoryCache.Remove(cacheKey);
                }
            }
        }

        public async Task<Tournament> CreateTournament(Tournament tournament, Guid memberKey, string memberName)
        {
            var createdTournament = await _tournamentRepository.CreateTournament(tournament, memberKey, memberName).ConfigureAwait(false);
            await ClearImportantCachesForTournament(createdTournament);
            return createdTournament;
        }

        public async Task<Tournament> UpdateTournament(Tournament tournament, Guid memberKey, string memberName)
        {
            var updatedTournament = await _tournamentRepository.UpdateTournament(tournament, memberKey, memberName).ConfigureAwait(false);
            await ClearImportantCachesForTournament(updatedTournament);
            return updatedTournament;
        }

        public async Task<Tournament> UpdateTeams(Tournament tournament, Guid memberKey, string memberUsername, string memberName)
        {
            var updatedTournament = await _tournamentRepository.UpdateTeams(tournament, memberKey, memberUsername, memberName);
            await ClearImportantCachesForTournament(updatedTournament);
            return updatedTournament;
        }

        public async Task<Tournament> UpdateSeasons(Tournament tournament, Guid memberKey, string memberUsername, string memberName)
        {
            var updatedTournament = await _tournamentRepository.UpdateSeasons(tournament, memberKey, memberUsername, memberName);
            await ClearImportantCachesForTournament(updatedTournament);
            return updatedTournament;
        }

        public async Task DeleteTournament(Tournament tournament, Guid memberKey, string memberName)
        {
            await _tournamentRepository.DeleteTournament(tournament, memberKey, memberName);
            await ClearImportantCachesForTournament(tournament);
        }
    }
}
