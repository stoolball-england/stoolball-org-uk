using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Caching;

namespace Stoolball.Matches
{
    public class MatchListingCacheClearer : IMatchListingCacheClearer
    {
        private readonly IReadThroughCache _readThroughCache;

        public MatchListingCacheClearer(IReadThroughCache readThroughCache) // IClubDataSource clubDataSource, IMatchFilterQueryStringSerializer matchFilterSerializer
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
        }

        private void ClearMatchListingCache(string granularCacheKey = null)
        {
            _readThroughCache.InvalidateCache(nameof(IMatchListingDataSource) + nameof(IMatchListingDataSource.ReadTotalMatches) + granularCacheKey);
            _readThroughCache.InvalidateCache(nameof(IMatchListingDataSource) + nameof(IMatchListingDataSource.ReadMatchListings) + granularCacheKey);
        }

        public async Task ClearCacheFor(Tournament tournament)
        {
            await ClearCacheFor(tournament, null);
        }

        public Task ClearCacheFor(Tournament tournamentBefore, Tournament tournamentAfter)
        {
            ClearMatchListingCache();

            var affectedTeams = new List<Guid>();
            if (tournamentBefore != null) { affectedTeams.AddRange(tournamentBefore.Teams.Where(t => t.Team != null && t.Team.TeamId.HasValue).Select(t => t.Team.TeamId.Value)); }
            if (tournamentAfter != null) { affectedTeams.AddRange(tournamentAfter.Teams.Where(t => t.Team != null && t.Team.TeamId.HasValue).Select(t => t.Team.TeamId.Value)); }
            foreach (var teamId in affectedTeams.Distinct())
            {
                ClearMatchListingCache("ForTeam" + teamId);
            }

            //var affectedTeams = tournament.Teams.Select(t => t.Team.TeamId.Value).ToList();
            //foreach (var teamId in affectedTeams)
            //{
            //    var filter = _matchFilterFactory.MatchesForTeams(new List<Guid> { teamId });
            //    var cacheKey = CachePolicy.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
            //    _cache.Remove(cacheKey);
            //}

            //if (affectedTeams.Any())
            //{
            //    var clubs = await _clubDataSource.ReadClubs(new ClubFilter { TeamIds = affectedTeams }).ConfigureAwait(false);
            //    foreach (var club in clubs)
            //    {
            //        var filter = _matchFilterFactory.MatchesForTeams(club.Teams.Select(x => x.TeamId.Value).ToList());
            //        var cacheKey = CachePolicy.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
            //        _cache.Remove(cacheKey);
            //    }
            //}

            //if (tournament.TournamentLocation != null)
            //{
            //    var filter = _matchFilterFactory.MatchesForMatchLocation(tournament.TournamentLocation.MatchLocationId.Value);
            //    var cacheKey = CachePolicy.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
            //    _cache.Remove(cacheKey);
            //}

            //if (tournament.Seasons != null)
            //{
            //    foreach (var season in tournament.Seasons)
            //    {
            //        var filter = _matchFilterFactory.MatchesForSeason(season.SeasonId.Value);
            //        var cacheKey = CachePolicy.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
            //        _cache.Remove(cacheKey);
            //    }
            //}

            //if (tournament != null)
            //{
            //    var filter = _matchFilterFactory.MatchesForTournament(tournament.TournamentId.Value);
            //    var cacheKey = CachePolicy.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
            //    _cache.Remove(cacheKey);
            //}

            return Task.CompletedTask;
        }

        public async Task ClearCacheFor(Match match)
        {
            await ClearCacheFor(match, null).ConfigureAwait(false);
        }

        public Task ClearCacheFor(Match matchBeforeUpdate, Match matchAfterUpdate)
        {
            ClearMatchListingCache();

            var affectedTeams = new List<Guid>();
            if (matchBeforeUpdate != null) { affectedTeams.AddRange(matchBeforeUpdate.Teams.Where(t => t.Team != null && t.Team.TeamId.HasValue).Select(t => t.Team.TeamId.Value)); }
            if (matchAfterUpdate != null) { affectedTeams.AddRange(matchAfterUpdate.Teams.Where(t => t.Team != null && t.Team.TeamId.HasValue).Select(t => t.Team.TeamId.Value)); }
            foreach (var teamId in affectedTeams.Distinct())
            {
                ClearMatchListingCache("ForTeam" + teamId);
            }

            //if (affectedTeams.Any())
            //{
            //    var clubs = await _clubDataSource.ReadClubs(new ClubFilter { TeamIds = affectedTeams }).ConfigureAwait(false);
            //    foreach (var club in clubs)
            //    {
            //        var filter = _matchFilterFactory.MatchesForTeams(club.Teams.Select(x => x.TeamId.Value).ToList());
            //        var cacheKey = CachePolicy.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
            //        _cache.Remove(cacheKey);
            //    }
            //}

            //if (match.MatchLocation != null)
            //{
            //    var filter = _matchFilterFactory.MatchesForMatchLocation(match.MatchLocation.MatchLocationId.Value);
            //    var cacheKey = CachePolicy.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
            //    _cache.Remove(cacheKey);
            //}

            //if (match.Season != null)
            //{
            //    var filter = _matchFilterFactory.MatchesForSeason(match.Season.SeasonId.Value);
            //    var cacheKey = CachePolicy.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
            //    _cache.Remove(cacheKey);
            //}

            //if (match.Tournament != null)
            //{
            //    var filter = _matchFilterFactory.MatchesForTournament(match.Tournament.TournamentId.Value);
            //    var cacheKey = CachePolicy.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
            //    _cache.Remove(cacheKey);
            //}

            return Task.CompletedTask;
        }

        public void ClearCacheForTeam(Guid teamId)
        {
            ClearMatchListingCache();
            ClearMatchListingCache("ForTeam" + teamId);
        }
    }
}
