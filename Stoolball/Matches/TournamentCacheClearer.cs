﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Caching;
using Stoolball.Clubs;

namespace Stoolball.Matches
{
    public class TournamentCacheClearer : ICacheClearer<Tournament>
    {
        private readonly IClubDataSource _clubDataSource;
        private readonly IMatchFilterFactory _matchFilterFactory;
        private readonly IMatchFilterSerializer _matchFilterSerializer;
        private readonly IClearableCache _cache;

        public TournamentCacheClearer(IClubDataSource clubDataSource,
            IMatchFilterFactory matchFilterFactory, IMatchFilterSerializer matchFilterSerializer, IClearableCache cache)
        {
            _clubDataSource = clubDataSource ?? throw new ArgumentNullException(nameof(clubDataSource));
            _matchFilterFactory = matchFilterFactory ?? throw new ArgumentNullException(nameof(matchFilterFactory));
            _matchFilterSerializer = matchFilterSerializer ?? throw new ArgumentNullException(nameof(matchFilterSerializer));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task ClearCacheFor(Tournament tournament)
        {
            if (tournament == null) { throw new ArgumentNullException(nameof(tournament)); }

            var affectedTeams = tournament.Teams.Select(t => t.Team.TeamId.Value).ToList();
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

            if (tournament.TournamentLocation != null)
            {
                var filter = _matchFilterFactory.MatchesForMatchLocation(tournament.TournamentLocation.MatchLocationId.Value);
                var cacheKey = CacheConstants.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
                _cache.Remove(cacheKey);
            }

            if (tournament.Seasons != null)
            {
                foreach (var season in tournament.Seasons)
                {
                    var filter = _matchFilterFactory.MatchesForSeason(season.SeasonId.Value);
                    var cacheKey = CacheConstants.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
                    _cache.Remove(cacheKey);
                }
            }

            if (tournament != null)
            {
                var filter = _matchFilterFactory.MatchesForTournament(tournament.TournamentId.Value);
                var cacheKey = CacheConstants.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
                _cache.Remove(cacheKey);
            }
        }
    }
}
