using System;
using System.Threading.Tasks;
using Stoolball.Caching;

namespace Stoolball.Matches
{
    public class MatchListingCacheClearer : IMatchListingCacheClearer
    {
        private readonly IReadThroughCache _readThroughCache;

        public MatchListingCacheClearer(IReadThroughCache readThroughCache) // IClubDataSource clubDataSource,IMatchFilterFactory matchFilterFactory, IMatchFilterQueryStringSerializer matchFilterSerializer
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
        }

        private void ClearUnfilteredMatchListingCache()
        {
            _readThroughCache.InvalidateCache(nameof(IMatchListingDataSource) + nameof(IMatchListingDataSource.ReadTotalMatches));
            _readThroughCache.InvalidateCache(nameof(IMatchListingDataSource) + nameof(IMatchListingDataSource.ReadMatchListings));
        }

        public Task ClearCacheFor(Tournament tournament)
        {
            ClearUnfilteredMatchListingCache();

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

        public Task ClearCacheFor(Match match)
        {
            ClearUnfilteredMatchListingCache();

            //var affectedTeams = match.Teams.Select(t => t.Team.TeamId.Value).ToList();
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
    }
}
