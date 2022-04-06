using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Stoolball.Caching;
using Stoolball.Clubs;

namespace Stoolball.Matches
{
    public class MatchCacheClearer : ICacheClearer<Match>
    {
        private readonly IClubDataSource _clubDataSource;
        private readonly IMatchFilterFactory _matchFilterFactory;
        private readonly IMatchFilterQueryStringSerializer _matchFilterSerializer;
        private readonly IMemoryCache _cache;

        public MatchCacheClearer(IClubDataSource clubDataSource, IMatchFilterFactory matchFilterFactory, IMatchFilterQueryStringSerializer matchFilterSerializer, IMemoryCache cache)
        {
            _clubDataSource = clubDataSource ?? throw new ArgumentNullException(nameof(clubDataSource));
            _matchFilterFactory = matchFilterFactory ?? throw new ArgumentNullException(nameof(matchFilterFactory));
            _matchFilterSerializer = matchFilterSerializer ?? throw new ArgumentNullException(nameof(matchFilterSerializer));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task ClearCacheFor(Match match)
        {
            if (match == null) { throw new ArgumentNullException(nameof(match)); }

            var affectedTeams = match.Teams.Select(t => t.Team.TeamId.Value).ToList();
            foreach (var teamId in affectedTeams)
            {
                var filter = _matchFilterFactory.MatchesForTeams(new List<Guid> { teamId });
                var cacheKey = CacheConstants.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
                _cache.Remove(cacheKey);
            }

            if (affectedTeams.Any())
            {
                var clubs = await _clubDataSource.ReadClubs(new ClubFilter { TeamIds = affectedTeams }).ConfigureAwait(false);
                foreach (var club in clubs)
                {
                    var filter = _matchFilterFactory.MatchesForTeams(club.Teams.Select(x => x.TeamId.Value).ToList());
                    var cacheKey = CacheConstants.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
                    _cache.Remove(cacheKey);
                }
            }

            if (match.MatchLocation != null)
            {
                var filter = _matchFilterFactory.MatchesForMatchLocation(match.MatchLocation.MatchLocationId.Value);
                var cacheKey = CacheConstants.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
                _cache.Remove(cacheKey);
            }

            if (match.Season != null)
            {
                var filter = _matchFilterFactory.MatchesForSeason(match.Season.SeasonId.Value);
                var cacheKey = CacheConstants.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
                _cache.Remove(cacheKey);
            }

            if (match.Tournament != null)
            {
                var filter = _matchFilterFactory.MatchesForTournament(match.Tournament.TournamentId.Value);
                var cacheKey = CacheConstants.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
                _cache.Remove(cacheKey);
            }
        }
    }
}
