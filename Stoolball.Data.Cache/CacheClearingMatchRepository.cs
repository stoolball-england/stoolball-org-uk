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
using Stoolball.Statistics;

namespace Stoolball.Data.Cache
{
    public class CacheClearingMatchRepository : SqlServerMatchRepository
    {
        private readonly IMatchFilterFactory _matchFilterFactory;
        private readonly IMatchFilterSerializer _matchFilterSerializer;
        private readonly IMemoryCache _memoryCache;

        public CacheClearingMatchRepository(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator,
            IRedirectsRepository redirectsRepository, IHtmlSanitizer htmlSanitiser, IMatchNameBuilder matchNameBuilder, IPlayerTypeSelector playerTypeSelector,
            IBowlingScorecardComparer bowlingScorecardComparer, IBattingScorecardComparer battingScorecardComparer, IPlayerRepository playerRepository, IDataRedactor dataRedactor,
            IStatisticsRepository statisticsRepository, IOversHelper oversHelper, IPlayerInMatchStatisticsBuilder playerInMatchStatisticsBuilder,
            IMatchFilterFactory matchFilterFactory, IMatchFilterSerializer matchFilterSerializer, IMemoryCache memoryCache)
            : base(databaseConnectionFactory, auditRepository, logger, routeGenerator, redirectsRepository, htmlSanitiser, matchNameBuilder, playerTypeSelector, bowlingScorecardComparer,
                  battingScorecardComparer, playerRepository, dataRedactor, statisticsRepository, oversHelper, playerInMatchStatisticsBuilder)
        {
            _matchFilterFactory = matchFilterFactory ?? throw new ArgumentNullException(nameof(matchFilterFactory));
            _matchFilterSerializer = matchFilterSerializer ?? throw new ArgumentNullException(nameof(matchFilterSerializer));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        private void ClearImportantCachesForMatch(Match match)
        {
            var affectedTeams = match.Teams.Select(t => t.Team.TeamId.Value);
            foreach (var teamId in affectedTeams)
            {
                var filter = _matchFilterFactory.MatchesForTeam(teamId);
                var cacheKey = CacheConstants.MatchListingsCacheKeyPrefix + _matchFilterSerializer.Serialize(filter.filter) + filter.sortOrder;
                _memoryCache.Remove(cacheKey);
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

        public async override Task<Match> CreateMatch(Match match, Guid memberKey, string memberName)
        {
            var createdMatch = await base.CreateMatch(match, memberKey, memberName).ConfigureAwait(false);
            ClearImportantCachesForMatch(createdMatch);
            return createdMatch;
        }

        public async override Task<Match> UpdateMatch(Match match, Guid memberKey, string memberName)
        {
            var updatedMatch = await base.UpdateMatch(match, memberKey, memberName).ConfigureAwait(false);
            ClearImportantCachesForMatch(updatedMatch);
            return updatedMatch;
        }

        public async override Task<Match> UpdateStartOfPlay(Match match, Guid memberKey, string memberName)
        {
            var updatedMatch = await base.UpdateStartOfPlay(match, memberKey, memberName).ConfigureAwait(false);
            ClearImportantCachesForMatch(updatedMatch);
            return updatedMatch;
        }

        public async override Task<Match> UpdateCloseOfPlay(Match match, Guid memberKey, string memberName)
        {
            var updatedMatch = await base.UpdateCloseOfPlay(match, memberKey, memberName).ConfigureAwait(false);
            ClearImportantCachesForMatch(updatedMatch);
            return updatedMatch;
        }

        public async override Task DeleteMatch(Match match, Guid memberKey, string memberName)
        {
            await base.DeleteMatch(match, memberKey, memberName).ConfigureAwait(false);
            ClearImportantCachesForMatch(match);
        }
    }
}
