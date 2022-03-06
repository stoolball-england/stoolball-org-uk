using System;
using Stoolball.Caching;
using Stoolball.Comments;
using Stoolball.Data.Cache;
using Stoolball.Data.SqlServer;
using Stoolball.Email;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Web.Caching;
using Stoolball.Web.Matches;
using Stoolball.Web.Statistics;
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace Stoolball.Web
{
    public class DependencyInjectionComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            if (composition is null)
            {
                throw new ArgumentNullException(nameof(composition));
            }

            // Utility classes
            composition.Register<IMatchNameBuilder, MatchNameBuilder>();
            composition.Register<IPlayerTypeSelector, PlayerTypeSelector>();
            composition.Register<IEditMatchHelper, EditMatchHelper>();
            composition.Register<IMatchValidator, MatchValidator>();
            composition.Register<IMatchResultEvaluator, MatchResultEvaluator>();
            composition.Register<IMatchInningsUrlParser, MatchInningsUrlParser>();
            composition.Register<IPlayerInningsScaffolder, PlayerInningsScaffolder>();
            composition.Register<IBowlingScorecardComparer, BowlingScorecardComparer>();
            composition.Register<IBattingScorecardComparer, BattingScorecardComparer>();
            composition.Register<IBackgroundTaskTracker, MemoryCacheBackgroundTaskTracker>();
            composition.Register<IOverSetScaffolder, OverSetScaffolder>();
            composition.Register<IPlayerInMatchStatisticsBuilder, PlayerInMatchStatisticsBuilder>();
            composition.Register<IPlayerIdentityFinder, PlayerIdentityFinder>();
            composition.Register<IStatisticsFilterFactory, StatisticsFilterFactory>();
            composition.Register<IStatisticsBreadcrumbBuilder, StatisticsBreadcrumbBuilder>();
            composition.Register<IContactDetailsParser, ContactDetailsParser>();
            composition.Register<IPlayerNameFormatter, PlayerNameFormatter>();
            composition.Register<IMatchInningsFactory, MatchInningsFactory>();
            composition.Register<IStoolballEntityRouteParser, StoolballEntityRouteParser>();

            // Caching with Polly
            composition.Register<IClearableCache, ClearableCacheWrapper>();
            composition.Register<ICacheClearer<Tournament>, TournamentCacheClearer>();
            composition.Register<ICacheClearer<Match>, MatchCacheClearer>();

            // Data sources for stoolball data.
            composition.Register<IPlayerRepository, SqlServerPlayerRepository>();
            composition.Register<ICommentsDataSource<Tournament>, CachedCommentsDataSource<Tournament>>();
            composition.Register<ICacheableCommentsDataSource<Tournament>, SqlServerTournamentCommentsDataSource>();
            composition.Register<IMatchRepository, SqlServerMatchRepository>();
            composition.Register<ITournamentRepository, SqlServerTournamentRepository>();
            composition.Register<IStatisticsRepository, SqlServerStatisticsRepository>();
            composition.Register<IPlayerSummaryStatisticsDataSource, CachedPlayerSummaryStatisticsDataSource>();
            composition.Register<ICacheablePlayerSummaryStatisticsDataSource, SqlServerPlayerSummaryStatisticsDataSource>();
            composition.Register<IPlayerPerformanceStatisticsDataSource, CachedPlayerPerformanceStatisticsDataSource>();
            composition.Register<ICacheablePlayerPerformanceStatisticsDataSource, SqlServerPlayerPerformanceStatisticsDataSource>();
            composition.Register<IBestPlayerAverageStatisticsDataSource, CachedBestPlayerAverageStatisticsDataSource>();
            composition.Register<ICacheableBestPlayerAverageStatisticsDataSource, SqlServerBestPlayerAverageStatisticsDataSource>();

            // Security checks
            composition.Register<Stoolball.Web.Security.IAuthorizationPolicy<Tournament>, TournamentAuthorizationPolicy>();
        }
    }
}