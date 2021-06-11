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
    public class CacheClearingMatchRepository : IMatchRepository
    {
        private readonly IWrappableMatchRepository _matchRepository;
        private readonly IClubDataSource _clubDataSource;
        private readonly IMatchFilterFactory _matchFilterFactory;
        private readonly IMatchFilterSerializer _matchFilterSerializer;
        private readonly IMemoryCache _memoryCache;

        public CacheClearingMatchRepository(IWrappableMatchRepository matchRepository, IClubDataSource clubDataSource, IMatchFilterFactory matchFilterFactory, IMatchFilterSerializer matchFilterSerializer, IMemoryCache memoryCache)
        {
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _clubDataSource = clubDataSource;
            _matchFilterFactory = matchFilterFactory ?? throw new ArgumentNullException(nameof(matchFilterFactory));
            _matchFilterSerializer = matchFilterSerializer ?? throw new ArgumentNullException(nameof(matchFilterSerializer));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        private async Task ClearImportantCachesForMatch(Match match)
        {
            var affectedTeams = match.Teams.Select(t => t.Team.TeamId.Value).ToList();
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

            if (match?.MatchLocation != null)
            {
                var filter = _matchFilterFactory.MatchesForMatchLocation(match.MatchLocation.MatchLocationId.Value);
                var cacheKey = CacheConstants.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
                _memoryCache.Remove(cacheKey);
            }

            if (match?.Season != null)
            {
                var filter = _matchFilterFactory.MatchesForSeason(match.Season.SeasonId.Value);
                var cacheKey = CacheConstants.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
                _memoryCache.Remove(cacheKey);
            }
        }

        public async Task<Match> CreateMatch(Match match, Guid memberKey, string memberName)
        {
            var createdMatch = await _matchRepository.CreateMatch(match, memberKey, memberName).ConfigureAwait(false);
            await ClearImportantCachesForMatch(createdMatch);
            return createdMatch;
        }

        public async Task<Match> UpdateMatch(Match match, Guid memberKey, string memberName)
        {
            var updatedMatch = await _matchRepository.UpdateMatch(match, memberKey, memberName).ConfigureAwait(false);
            await ClearImportantCachesForMatch(updatedMatch);
            return updatedMatch;
        }

        public async Task<Match> UpdateStartOfPlay(Match match, Guid memberKey, string memberName)
        {
            var updatedMatch = await _matchRepository.UpdateStartOfPlay(match, memberKey, memberName).ConfigureAwait(false);
            await ClearImportantCachesForMatch(updatedMatch);
            return updatedMatch;
        }

        public async Task<Match> UpdateCloseOfPlay(Match match, Guid memberKey, string memberName)
        {
            var updatedMatch = await _matchRepository.UpdateCloseOfPlay(match, memberKey, memberName).ConfigureAwait(false);
            await ClearImportantCachesForMatch(updatedMatch);
            return updatedMatch;
        }

        public async Task DeleteMatch(Match match, Guid memberKey, string memberName)
        {
            await _matchRepository.DeleteMatch(match, memberKey, memberName).ConfigureAwait(false);
            await ClearImportantCachesForMatch(match);
        }

        public async Task<MatchInnings> UpdateBowlingScorecard(Match match, Guid matchInningsId, Guid memberKey, string memberName)
        {
            return await _matchRepository.UpdateBowlingScorecard(match, matchInningsId, memberKey, memberName).ConfigureAwait(false);
        }

        public async Task<MatchInnings> UpdateBattingScorecard(Match match, Guid matchInningsId, Guid memberKey, string memberName)
        {
            return await _matchRepository.UpdateBattingScorecard(match, matchInningsId, memberKey, memberName).ConfigureAwait(false);
        }
    }
}
