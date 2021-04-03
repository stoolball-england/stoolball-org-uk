using System;
using System.Linq;
using System.Threading.Tasks;
using Ganss.XSS;
using Microsoft.Extensions.Caching.Memory;
using Stoolball.Data.SqlServer;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Teams;

namespace Stoolball.Data.Cache
{
    public class CacheClearingTournamentRepository : SqlServerTournamentRepository
    {
        private readonly IMatchFilterFactory _matchFilterFactory;
        private readonly IMatchFilterSerializer _matchFilterSerializer;
        private readonly IMemoryCache _memoryCache;

        public CacheClearingTournamentRepository(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator,
            IRedirectsRepository redirectsRepository, ITeamRepository teamRepository, IHtmlSanitizer htmlSanitiser, IDataRedactor dataRedactor,
            IMatchFilterFactory matchFilterFactory, IMatchFilterSerializer matchFilterSerializer, IMemoryCache memoryCache)
            : base(databaseConnectionFactory, auditRepository, logger, routeGenerator, redirectsRepository, teamRepository, htmlSanitiser, dataRedactor)
        {
            _matchFilterFactory = matchFilterFactory ?? throw new ArgumentNullException(nameof(matchFilterFactory));
            _matchFilterSerializer = matchFilterSerializer ?? throw new ArgumentNullException(nameof(matchFilterSerializer));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        private void ClearImportantCachesForTournament(Tournament tournament)
        {
            var affectedTeams = tournament.Teams.Select(t => t.Team.TeamId.Value);
            foreach (var teamId in affectedTeams)
            {
                var filter = _matchFilterFactory.MatchesForTeam(teamId);
                var cacheKey = CacheConstants.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
                _memoryCache.Remove(cacheKey);
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

        public async override Task<Tournament> CreateTournament(Tournament tournament, Guid memberKey, string memberName)
        {
            var createdTournament = await base.CreateTournament(tournament, memberKey, memberName).ConfigureAwait(false);
            ClearImportantCachesForTournament(createdTournament);
            return createdTournament;
        }

        public async override Task<Tournament> UpdateTournament(Tournament tournament, Guid memberKey, string memberName)
        {
            var updatedTournament = await base.UpdateTournament(tournament, memberKey, memberName).ConfigureAwait(false);
            ClearImportantCachesForTournament(updatedTournament);
            return updatedTournament;
        }

        public async override Task<Tournament> UpdateTeams(Tournament tournament, Guid memberKey, string memberUsername, string memberName)
        {
            var updatedTournament = await base.UpdateTeams(tournament, memberKey, memberUsername, memberName);
            ClearImportantCachesForTournament(updatedTournament);
            return updatedTournament;
        }

        public async override Task<Tournament> UpdateSeasons(Tournament tournament, Guid memberKey, string memberUsername, string memberName)
        {
            var updatedTournament = await base.UpdateSeasons(tournament, memberKey, memberUsername, memberName);
            ClearImportantCachesForTournament(updatedTournament);
            return updatedTournament;
        }

        public async override Task DeleteTournament(Tournament tournament, Guid memberKey, string memberName)
        {
            await base.DeleteTournament(tournament, memberKey, memberName);
            ClearImportantCachesForTournament(tournament);
        }
    }
}
