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

        public async Task ClearCacheForTournament(Tournament tournament)
        {
            await ClearCacheForTournament(tournament, null);
        }

        public async Task ClearCacheForTournament(Tournament tournamentBefore, Tournament tournamentAfter)
        {
            ClearMatchListingCache();

            var affectedTeams = new List<Guid>();
            if (tournamentBefore != null) { affectedTeams.AddRange(tournamentBefore.Teams.Where(t => t.Team != null && t.Team.TeamId.HasValue).Select(t => t.Team.TeamId.Value)); }
            if (tournamentAfter != null) { affectedTeams.AddRange(tournamentAfter.Teams.Where(t => t.Team != null && t.Team.TeamId.HasValue).Select(t => t.Team.TeamId.Value)); }
            await ClearCacheForAffectedTeams(affectedTeams).ConfigureAwait(false);

            var affectedMatchLocations = new List<Guid>();
            if (tournamentBefore?.TournamentLocation?.MatchLocationId != null) { affectedMatchLocations.Add(tournamentBefore.TournamentLocation.MatchLocationId.Value); }
            if (tournamentAfter?.TournamentLocation?.MatchLocationId != null) { affectedMatchLocations.Add(tournamentAfter.TournamentLocation.MatchLocationId.Value); }
            ClearCacheForAffectedMatchLocations(affectedMatchLocations);

            var affectedSeasons = new List<Guid>();
            if (tournamentBefore != null) { affectedSeasons.AddRange(tournamentBefore.Seasons.Where(s => s.SeasonId.HasValue).Select(s => s.SeasonId.Value)); }
            if (tournamentAfter != null) { affectedSeasons.AddRange(tournamentAfter.Seasons.Where(s => s.SeasonId.HasValue).Select(s => s.SeasonId.Value)); }
            ClearCacheForAffectedSeasons(affectedSeasons);

            var affectedTournaments = new List<Guid>();
            if (tournamentBefore?.TournamentId != null) { affectedTournaments.Add(tournamentBefore.TournamentId.Value); }
            if (tournamentAfter?.TournamentId != null) { affectedTournaments.Add(tournamentAfter.TournamentId.Value); }
            ClearCacheForAffectedTournaments(affectedTournaments);
        }

        public async Task ClearCacheForMatch(Match match)
        {
            await ClearCacheForMatch(match, null).ConfigureAwait(false);
        }

        public async Task ClearCacheForMatch(Match matchBefore, Match matchAfter)
        {
            ClearMatchListingCache();

            var affectedTeams = new List<Guid>();
            if (matchBefore != null) { affectedTeams.AddRange(matchBefore.Teams.Where(t => t.Team != null && t.Team.TeamId.HasValue).Select(t => t.Team.TeamId.Value)); }
            if (matchAfter != null) { affectedTeams.AddRange(matchAfter.Teams.Where(t => t.Team != null && t.Team.TeamId.HasValue).Select(t => t.Team.TeamId.Value)); }
            await ClearCacheForAffectedTeams(affectedTeams).ConfigureAwait(false);

            var affectedMatchLocations = new List<Guid>();
            if (matchBefore?.MatchLocation?.MatchLocationId != null) { affectedMatchLocations.Add(matchBefore.MatchLocation.MatchLocationId.Value); }
            if (matchAfter?.MatchLocation?.MatchLocationId != null) { affectedMatchLocations.Add(matchAfter.MatchLocation.MatchLocationId.Value); }
            ClearCacheForAffectedMatchLocations(affectedMatchLocations);

            var affectedSeasons = new List<Guid>();
            if (matchBefore?.Season?.SeasonId != null) { affectedSeasons.Add(matchBefore.Season.SeasonId.Value); }
            if (matchAfter?.Season?.SeasonId != null) { affectedSeasons.Add(matchAfter.Season.SeasonId.Value); }
            ClearCacheForAffectedSeasons(affectedSeasons);

            var affectedTournaments = new List<Guid>();
            if (matchBefore?.Tournament?.TournamentId != null) { affectedTournaments.Add(matchBefore.Tournament.TournamentId.Value); }
            if (matchAfter?.Tournament?.TournamentId != null) { affectedTournaments.Add(matchAfter.Tournament.TournamentId.Value); }
            ClearCacheForAffectedTournaments(affectedTournaments);
        }

        public async Task ClearCacheForTeam(Guid teamId)
        {
            ClearMatchListingCache();
            await ClearCacheForAffectedTeams(new List<Guid> { teamId });
        }

        public void ClearCacheForMatchLocation(Guid matchLocationId)
        {
            ClearMatchListingCache();
            ClearCacheForAffectedMatchLocations(new List<Guid> { matchLocationId });
        }

        public void ClearCacheForSeason(Guid seasonId)
        {
            ClearMatchListingCache();
            ClearCacheForAffectedSeasons(new List<Guid> { seasonId });
        }

        public void ClearCacheForTournamentMatches(Guid tournamentId)
        {
            ClearCacheForAffectedTournaments(new List<Guid> { tournamentId });
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

        private void ClearCacheForAffectedMatchLocations(List<Guid> affectedMatchLocations)
        {
            if (affectedMatchLocations.Any())
            {
                affectedMatchLocations = affectedMatchLocations.Distinct().ToList();
                foreach (var matchLocationId in affectedMatchLocations)
                {
                    ClearMatchListingCache("ForMatchLocation" + matchLocationId);
                }
            }
        }

        private void ClearCacheForAffectedSeasons(List<Guid> affectedSeasons)
        {
            if (affectedSeasons.Any())
            {
                affectedSeasons = affectedSeasons.Distinct().ToList();
                foreach (var seasonId in affectedSeasons)
                {
                    ClearMatchListingCache("ForSeason" + seasonId);
                }
            }
        }

        private void ClearCacheForAffectedTournaments(List<Guid> affectedTournaments)
        {
            if (affectedTournaments.Any())
            {
                affectedTournaments = affectedTournaments.Distinct().ToList();
                foreach (var tournamentId in affectedTournaments)
                {
                    ClearMatchListingCache("ForTournament" + tournamentId);
                }
            }
        }
    }
}
