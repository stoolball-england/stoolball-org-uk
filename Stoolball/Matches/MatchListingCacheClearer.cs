using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Caching;
using Stoolball.Clubs;

namespace Stoolball.Matches
{
    public class MatchListingCacheClearer : IMatchListingCacheClearer
    {
        private readonly IReadThroughCache _readThroughCache;
        private readonly IClubDataSource _clubDataSource;

        public MatchListingCacheClearer(IReadThroughCache readThroughCache, IClubDataSource clubDataSource)//, IMatchFilterQueryStringSerializer matchFilterSerializer
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
            _clubDataSource = clubDataSource ?? throw new ArgumentNullException(nameof(clubDataSource));
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

        public async Task ClearCacheFor(Tournament tournamentBefore, Tournament tournamentAfter)
        {
            ClearMatchListingCache();

            var affectedTeams = new List<Guid>();
            if (tournamentBefore != null) { affectedTeams.AddRange(tournamentBefore.Teams.Where(t => t.Team != null && t.Team.TeamId.HasValue).Select(t => t.Team.TeamId.Value)); }
            if (tournamentAfter != null) { affectedTeams.AddRange(tournamentAfter.Teams.Where(t => t.Team != null && t.Team.TeamId.HasValue).Select(t => t.Team.TeamId.Value)); }
            await ClearCacheForAffectedTeams(affectedTeams).ConfigureAwait(false);

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
        }

        public async Task ClearCacheFor(Match match)
        {
            await ClearCacheFor(match, null).ConfigureAwait(false);
        }

        public async Task ClearCacheFor(Match matchBeforeUpdate, Match matchAfterUpdate)
        {
            ClearMatchListingCache();

            var affectedTeams = new List<Guid>();
            if (matchBeforeUpdate != null) { affectedTeams.AddRange(matchBeforeUpdate.Teams.Where(t => t.Team != null && t.Team.TeamId.HasValue).Select(t => t.Team.TeamId.Value)); }
            if (matchAfterUpdate != null) { affectedTeams.AddRange(matchAfterUpdate.Teams.Where(t => t.Team != null && t.Team.TeamId.HasValue).Select(t => t.Team.TeamId.Value)); }
            await ClearCacheForAffectedTeams(affectedTeams).ConfigureAwait(false);

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
        }

        public async Task ClearCacheForTeam(Guid teamId)
        {
            ClearMatchListingCache();
            await ClearCacheForAffectedTeams(new List<Guid> { teamId });
        }

        private async Task ClearCacheForAffectedTeams(List<Guid> affectedTeams)
        {
            if (affectedTeams.Any())
            {
                affectedTeams = affectedTeams.Distinct().ToList();
                foreach (var teamId in affectedTeams)
                {
                    ClearMatchListingCache("ForTeam" + teamId);
                }

                var clubs = await _clubDataSource.ReadClubs(new ClubFilter { TeamIds = affectedTeams }).ConfigureAwait(false);
                foreach (var club in clubs)
                {
                    ClearMatchListingCache("ForTeams" + string.Join("--", club.Teams.Select(x => x.TeamId.Value).OrderBy(x => x.ToString())));
                }
            }
        }
    }
}
