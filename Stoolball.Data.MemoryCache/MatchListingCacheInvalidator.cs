﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Clubs;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;

namespace Stoolball.Data.MemoryCache
{
    public class MatchListingCacheInvalidator : IMatchListingCacheInvalidator
    {
        private readonly IReadThroughCache _readThroughCache;
        private readonly IClubDataSource _clubDataSource;

        public MatchListingCacheInvalidator(IReadThroughCache readThroughCache, IClubDataSource clubDataSource)//, IMatchFilterQueryStringSerializer matchFilterSerializer
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
            _clubDataSource = clubDataSource ?? throw new ArgumentNullException(nameof(clubDataSource));
        }

        private void InvalidateMatchListingCache(string? granularCacheKey = null)
        {
            _readThroughCache.InvalidateCache(nameof(IMatchListingDataSource) + nameof(IMatchListingDataSource.ReadTotalMatches) + granularCacheKey);
            _readThroughCache.InvalidateCache(nameof(IMatchListingDataSource) + nameof(IMatchListingDataSource.ReadMatchListings) + granularCacheKey);
        }

        public async Task InvalidateCacheForTournament(Tournament tournament)
        {
            await InvalidateCacheForTournament(tournament, null);
        }

        public async Task InvalidateCacheForTournament(Tournament tournamentBefore, Tournament? tournamentAfter)
        {
            InvalidateMatchListingCache();

            var affectedTeams = new List<Guid>();
            if (tournamentBefore != null) { affectedTeams.AddRange(tournamentBefore.Teams.Where(t => t.Team != null && t.Team.TeamId.HasValue).Select(t => t.Team!.TeamId!.Value)); }
            if (tournamentAfter != null) { affectedTeams.AddRange(tournamentAfter.Teams.Where(t => t.Team != null && t.Team.TeamId.HasValue).Select(t => t.Team!.TeamId!.Value)); }
            await InvalidateCacheForAffectedTeams(affectedTeams).ConfigureAwait(false);

            var affectedMatchLocations = new List<Guid>();
            if (tournamentBefore?.TournamentLocation?.MatchLocationId != null) { affectedMatchLocations.Add(tournamentBefore.TournamentLocation.MatchLocationId.Value); }
            if (tournamentAfter?.TournamentLocation?.MatchLocationId != null) { affectedMatchLocations.Add(tournamentAfter.TournamentLocation.MatchLocationId.Value); }
            InvalidateCacheForAffectedMatchLocations(affectedMatchLocations);

            var affectedSeasons = new List<Guid>();
            if (tournamentBefore != null) { affectedSeasons.AddRange(tournamentBefore.Seasons.Where(s => s.SeasonId.HasValue).Select(s => s.SeasonId!.Value)); }
            if (tournamentAfter != null) { affectedSeasons.AddRange(tournamentAfter.Seasons.Where(s => s.SeasonId.HasValue).Select(s => s.SeasonId!.Value)); }
            InvalidateCacheForAffectedSeasons(affectedSeasons);

            var affectedTournaments = new List<Guid>();
            if (tournamentBefore?.TournamentId != null) { affectedTournaments.Add(tournamentBefore.TournamentId.Value); }
            if (tournamentAfter?.TournamentId != null) { affectedTournaments.Add(tournamentAfter.TournamentId.Value); }
            InvalidateCacheForAffectedTournaments(affectedTournaments);
        }

        public async Task InvalidateCacheForMatch(Match match)
        {
            await InvalidateCacheForMatch(match, null).ConfigureAwait(false);
        }

        public async Task InvalidateCacheForMatch(Match matchBefore, Match? matchAfter)
        {
            InvalidateMatchListingCache();

            var affectedTeams = new List<Guid>();
            if (matchBefore != null) { affectedTeams.AddRange(matchBefore.Teams.Where(t => t.Team != null && t.Team.TeamId.HasValue).Select(t => t.Team!.TeamId!.Value)); }
            if (matchAfter != null) { affectedTeams.AddRange(matchAfter.Teams.Where(t => t.Team != null && t.Team.TeamId.HasValue).Select(t => t.Team!.TeamId!.Value)); }
            await InvalidateCacheForAffectedTeams(affectedTeams).ConfigureAwait(false);

            var affectedMatchLocations = new List<Guid>();
            if (matchBefore?.MatchLocation?.MatchLocationId != null) { affectedMatchLocations.Add(matchBefore.MatchLocation.MatchLocationId.Value); }
            if (matchAfter?.MatchLocation?.MatchLocationId != null) { affectedMatchLocations.Add(matchAfter.MatchLocation.MatchLocationId.Value); }
            InvalidateCacheForAffectedMatchLocations(affectedMatchLocations);

            var affectedSeasons = new List<Guid>();
            if (matchBefore?.Season?.SeasonId != null) { affectedSeasons.Add(matchBefore.Season.SeasonId.Value); }
            if (matchAfter?.Season?.SeasonId != null) { affectedSeasons.Add(matchAfter.Season.SeasonId.Value); }
            InvalidateCacheForAffectedSeasons(affectedSeasons);

            var affectedTournaments = new List<Guid>();
            if (matchBefore?.Tournament?.TournamentId != null) { affectedTournaments.Add(matchBefore.Tournament.TournamentId.Value); }
            if (matchAfter?.Tournament?.TournamentId != null) { affectedTournaments.Add(matchAfter.Tournament.TournamentId.Value); }
            InvalidateCacheForAffectedTournaments(affectedTournaments);
        }

        public async Task InvalidateCacheForTeam(Guid teamId)
        {
            InvalidateMatchListingCache();
            await InvalidateCacheForAffectedTeams(new List<Guid> { teamId });
        }

        public void InvalidateCacheForMatchLocation(Guid matchLocationId)
        {
            InvalidateMatchListingCache();
            InvalidateCacheForAffectedMatchLocations(new List<Guid> { matchLocationId });
        }

        public void InvalidateCacheForSeason(Guid seasonId)
        {
            InvalidateMatchListingCache();
            InvalidateCacheForAffectedSeasons(new List<Guid> { seasonId });
        }

        public void InvalidateCacheForTournamentMatches(Guid tournamentId)
        {
            InvalidateCacheForAffectedTournaments(new List<Guid> { tournamentId });
        }

        private async Task InvalidateCacheForAffectedTeams(List<Guid> affectedTeams)
        {
            if (affectedTeams.Any())
            {
                affectedTeams = affectedTeams.Distinct().ToList();
                foreach (var teamId in affectedTeams)
                {
                    InvalidateMatchListingCache("ForTeam" + teamId);
                }

                var filter = new ClubFilter();
                filter.TeamIds.AddRange(affectedTeams);
                var clubs = await _clubDataSource.ReadClubs(filter).ConfigureAwait(false);
                foreach (var club in clubs)
                {
                    InvalidateMatchListingCache("ForTeams" + string.Join("--", club.Teams.Select(x => x.TeamId).OfType<Guid>().OrderBy(x => x.ToString())));
                }
            }
        }

        private void InvalidateCacheForAffectedMatchLocations(List<Guid> affectedMatchLocations)
        {
            if (affectedMatchLocations.Any())
            {
                affectedMatchLocations = affectedMatchLocations.Distinct().ToList();
                foreach (var matchLocationId in affectedMatchLocations)
                {
                    InvalidateMatchListingCache("ForMatchLocation" + matchLocationId);
                }
            }
        }

        private void InvalidateCacheForAffectedSeasons(List<Guid> affectedSeasons)
        {
            if (affectedSeasons.Any())
            {
                affectedSeasons = affectedSeasons.Distinct().ToList();
                foreach (var seasonId in affectedSeasons)
                {
                    InvalidateMatchListingCache("ForSeason" + seasonId);
                }
            }
        }

        private void InvalidateCacheForAffectedTournaments(List<Guid> affectedTournaments)
        {
            if (affectedTournaments.Any())
            {
                affectedTournaments = affectedTournaments.Distinct().ToList();
                foreach (var tournamentId in affectedTournaments)
                {
                    InvalidateMatchListingCache("ForTournament" + tournamentId);
                }
            }
        }
    }
}
