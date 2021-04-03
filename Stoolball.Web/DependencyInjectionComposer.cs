using System;
using Ganss.XSS;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Caching;
using Polly.Caching.Memory;
using Polly.Registry;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Data.Cache;
using Stoolball.Data.SqlServer;
using Stoolball.Dates;
using Stoolball.Email;
using Stoolball.Html;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.SocialMedia;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators;
using Stoolball.Web.Clubs;
using Stoolball.Web.Competitions;
using Stoolball.Web.Configuration;
using Stoolball.Web.Logging;
using Stoolball.Web.Matches;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Stoolball.Web.Statistics;
using Stoolball.Web.Teams;
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
            composition.Register<ILogger, UmbracoLogWrapper>();
            composition.Register<Email.IEmailFormatter, Email.EmailFormatter>();
            composition.Register<Email.IEmailSender, Email.EmailSender>();
            composition.Register<IEmailProtector, EmailProtector>();
            composition.Register<IVerificationToken, VerificationToken>();
            composition.Register<IAuditRepository, SqlServerAuditRepository>();
            composition.Register<IRouteGenerator, RouteGenerator>();
            composition.Register<IRouteNormaliser, RouteNormaliser>();
            composition.Register<IApiKeyProvider, ConfigApiKeyProvider>();
            composition.Register<IDateTimeFormatter, DateTimeFormatter>();
            composition.Register<ISeasonEstimator, SeasonEstimator>();
            composition.Register<IHtmlSanitizer, HtmlSanitizer>();
            composition.Register<IHtmlFormatter, Stoolball.Html.HtmlFormatter>();
            composition.Register<ICreateMatchSeasonSelector, CreateMatchSeasonSelector>();
            composition.Register<IMatchNameBuilder, MatchNameBuilder>();
            composition.Register<IPlayerTypeSelector, PlayerTypeSelector>();
            composition.Register<IEditMatchHelper, EditMatchHelper>();
            composition.Register<IMemberGroupHelper, MemberGroupHelper>();
            composition.Register<IMatchResultEvaluator, MatchResultEvaluator>();
            composition.Register<IMatchInningsUrlParser, MatchInningsUrlParser>();
            composition.Register<IPlayerInningsScaffolder, PlayerInningsScaffolder>();
            composition.Register<IBowlingScorecardComparer, BowlingScorecardComparer>();
            composition.Register<IBattingScorecardComparer, BattingScorecardComparer>();
            composition.Register<IYouTubeUrlNormaliser, YouTubeUrlNormaliser>();
            composition.Register<IDataRedactor, DataRedactor>();
            composition.Register<IPostSaveRedirector, PostSaveRedirector>();
            composition.Register<IBowlingFiguresCalculator, BowlingFiguresCalculator>();
            composition.Register<IBackgroundTaskTracker, MemoryCacheBackgroundTaskTracker>();
            composition.Register<IOverSetScaffolder, OverSetScaffolder>();
            composition.Register<IPlayerInMatchStatisticsBuilder, PlayerInMatchStatisticsBuilder>();
            composition.Register<IPlayerIdentityFinder, PlayerIdentityFinder>();
            composition.Register<IOversHelper, OversHelper>();
            composition.Register<IStatisticsFilterUrlParser, StatisticsFilterUrlParser>();
            composition.Register<IStatisticsBreadcrumbBuilder, StatisticsBreadcrumbBuilder>();
            composition.Register<IContactDetailsParser, ContactDetailsParser>();
            composition.Register<IMatchesRssQueryStringParser, MatchesRssQueryStringParser>();
            composition.Register<IMatchFilterSerializer, MatchFilterQueryStringSerializer>();
            composition.Register<IStatisticsFilterSerializer, StatisticsFilterQueryStringSerializer>();

            // Data migration from the old Stoolball England website
            composition.Register<IAuditHistoryBuilder, AuditHistoryBuilder>();
            composition.Register<IRedirectsDataMigrator, SkybrudRedirectsDataMigrator>();
            composition.Register<IClubDataMigrator, SqlServerClubDataMigrator>();
            composition.Register<ISchoolDataMigrator, SqlServerSchoolDataMigrator>();
            composition.Register<IMatchLocationDataMigrator, SqlServerMatchLocationDataMigrator>();
            composition.Register<ITeamDataMigrator, SqlServerTeamDataMigrator>();
            composition.Register<ICompetitionDataMigrator, SqlServerCompetitionDataMigrator>();
            composition.Register<IMatchDataMigrator, SqlServerMatchDataMigrator>();
            composition.Register<ITournamentDataMigrator, SqlServerTournamentDataMigrator>();
            composition.Register<IPlayerDataMigrator, SqlServerPlayerDataMigrator>();
            composition.Register<IPlayerPerformanceDataMigrator, SqlServerPlayerPerformanceDataMigrator>();
            composition.Register<IMatchAwardDataMigrator, SqlServerMatchAwardDataMigrator>();
            composition.Register<IMatchCommentDataMigrator, SqlServerMatchCommentDataMigrator>();
            composition.Register<IMatchCommentSubscriptionDataMigrator, SqlServerMatchCommentSubscriptionDataMigrator>();
            composition.Register<ICompetitionSubscriptionDataMigrator, SqlServerCompetitionSubscriptionDataMigrator>();
            composition.Register<IUmbracoFormsDataMigrator, UmbracoFormsDataMigrator>();

            // Controllers for stoolball data pages. Register the concrete class since it'll never need to 
            // be injected anywhere except the one place where it's serving a page of content.
            composition.Register<IStoolballRouteTypeMapper, StoolballRouteTypeMapper>();
            composition.Register<IStoolballRouterController, StoolballRouterController>();

            // Caching with Polly
            composition.Register<IMemoryCache, MemoryCache>(Lifetime.Singleton);
            composition.Register<IOptions<MemoryCacheOptions>, MemoryCacheOptions>(Lifetime.Singleton);
            composition.Register<IAsyncCacheProvider, MemoryCacheProvider>(Lifetime.Singleton);
            composition.Register<IReadOnlyPolicyRegistry<string>>((serviceProvider) =>
            {
                var registry = new PolicyRegistry();
                var memoryCacheProvider = serviceProvider.GetInstance<IAsyncCacheProvider>();
                var logger = serviceProvider.GetInstance<ILogger>();
                var cachePolicy = Policy.CacheAsync(memoryCacheProvider, TimeSpan.FromMinutes(120), (context, key, ex) =>
                {
                    logger.Error(typeof(IAsyncCacheProvider), ex, "Cache provider for key {key}, threw exception: {ex}.", key, ex.Message);
                });

                registry.Add(CacheConstants.StatisticsPolicy, cachePolicy);
                registry.Add(CacheConstants.MatchesPolicy, cachePolicy);
                return registry;

            }, Lifetime.Singleton);

            // Data sources for stoolball data.
            composition.Register<IDatabaseConnectionFactory, UmbracoDatabaseConnectionFactory>();
            composition.Register<IRedirectsRepository, SkybrudRedirectsRepository>();
            composition.Register<IClubDataSource, SqlServerClubDataSource>();
            composition.Register<IClubRepository, SqlServerClubRepository>();
            composition.Register<ITeamDataSource, SqlServerTeamDataSource>();
            composition.Register<ITeamListingDataSource, SqlServerTeamListingDataSource>();
            composition.Register<ITeamRepository, SqlServerTeamRepository>();
            composition.Register<IPlayerDataSource, SqlServerPlayerDataSource>();
            composition.Register<IPlayerRepository, SqlServerPlayerRepository>();
            composition.Register<IMatchLocationDataSource, SqlServerMatchLocationDataSource>();
            composition.Register<IMatchLocationRepository, SqlServerMatchLocationRepository>();
            composition.Register<ICompetitionDataSource, SqlServerCompetitionDataSource>();
            composition.Register<ICompetitionRepository, SqlServerCompetitionRepository>();
            composition.Register<ISeasonDataSource, SqlServerSeasonDataSource>();
            composition.Register<ISeasonRepository, SqlServerSeasonRepository>();
            composition.Register<IMatchDataSource, SqlServerMatchDataSource>();
            composition.Register<IMatchListingDataSource, CachedMatchListingDataSource>();
            composition.Register<ICacheableMatchListingDataSource, SqlServerMatchListingDataSource>();
            composition.Register<ICommentsDataSource<Match>, SqlServerMatchCommentsDataSource>();
            composition.Register<ICommentsDataSource<Tournament>, SqlServerTournamentCommentsDataSource>();
            composition.Register<IMatchRepository, CacheClearingMatchRepository>();
            composition.Register<ITournamentDataSource, SqlServerTournamentDataSource>();
            composition.Register<ITournamentRepository, SqlServerTournamentRepository>();
            composition.Register<IStatisticsDataSource, CachedStatisticsDataSource>();
            composition.Register<ICacheableStatisticsDataSource, SqlServerStatisticsDataSource>();
            composition.Register<IStatisticsRepository, SqlServerStatisticsRepository>();
            composition.Register<IInningsStatisticsDataSource, SqlServerInningsStatisticsDataSource>();
            composition.Register<IMatchFilterFactory, MatchFilterFactory>();

            // Security checks
            composition.Register<IAuthorizationPolicy<Club>, ClubAuthorizationPolicy>();
            composition.Register<IAuthorizationPolicy<Competition>, CompetitionAuthorizationPolicy>();
            composition.Register<IAuthorizationPolicy<MatchLocation>, MatchLocationAuthorizationPolicy>();
            composition.Register<IAuthorizationPolicy<Match>, MatchAuthorizationPolicy>();
            composition.Register<IAuthorizationPolicy<Tournament>, TournamentAuthorizationPolicy>();
            composition.Register<IAuthorizationPolicy<Team>, TeamAuthorizationPolicy>();
        }
    }
}