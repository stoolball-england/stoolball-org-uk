using System;
using Stoolball.Caching;
using Stoolball.Clubs;
using Stoolball.Comments;
using Stoolball.Competitions;
using Stoolball.Data.Cache;
using Stoolball.Data.SqlServer;
using Stoolball.Email;
using Stoolball.Html;
using Stoolball.Listings;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Schools;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Account;
using Stoolball.Web.Caching;
using Stoolball.Web.Clubs;
using Stoolball.Web.Competitions;
using Stoolball.Web.Logging;
using Stoolball.Web.Matches;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.Routing;
using Stoolball.Web.Schools;
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
            composition.Register<IAuditRepository, SqlServerAuditRepository>();
            composition.Register<IRouteGenerator, RouteGenerator>();
            composition.Register<ISeasonEstimator, SeasonEstimator>();
            composition.Register<ICreateMatchSeasonSelector, CreateMatchSeasonSelector>();
            composition.Register<IMatchNameBuilder, MatchNameBuilder>();
            composition.Register<IPlayerTypeSelector, PlayerTypeSelector>();
            composition.Register<IEditMatchHelper, EditMatchHelper>();
            composition.Register<IMatchValidator, MatchValidator>();
            composition.Register<IMemberGroupHelper, MemberGroupHelper>();
            composition.Register<IMatchResultEvaluator, MatchResultEvaluator>();
            composition.Register<IMatchInningsUrlParser, MatchInningsUrlParser>();
            composition.Register<IPlayerInningsScaffolder, PlayerInningsScaffolder>();
            composition.Register<IBowlingScorecardComparer, BowlingScorecardComparer>();
            composition.Register<IBattingScorecardComparer, BattingScorecardComparer>();
            composition.Register<IDataRedactor, DataRedactor>();
            composition.Register<IPostSaveRedirector, PostSaveRedirector>();
            composition.Register<IBackgroundTaskTracker, MemoryCacheBackgroundTaskTracker>();
            composition.Register<IOverSetScaffolder, OverSetScaffolder>();
            composition.Register<IPlayerInMatchStatisticsBuilder, PlayerInMatchStatisticsBuilder>();
            composition.Register<IPlayerIdentityFinder, PlayerIdentityFinder>();
            composition.Register<IStatisticsFilterFactory, StatisticsFilterFactory>();
            composition.Register<IStatisticsBreadcrumbBuilder, StatisticsBreadcrumbBuilder>();
            composition.Register<IContactDetailsParser, ContactDetailsParser>();
            composition.Register<IMatchesRssQueryStringParser, MatchesRssQueryStringParser>();
            composition.Register<IMatchFilterQueryStringSerializer, MatchFilterQueryStringSerializer>();
            composition.Register<IStatisticsFilterQueryStringSerializer, StatisticsFilterQueryStringSerializer>();
            composition.Register<ITeamListingFilterSerializer, TeamListingFilterQueryStringSerializer>();
            composition.Register<ICompetitionFilterSerializer, CompetitionFilterQueryStringSerializer>();
            composition.Register<IBadLanguageFilter, BadLanguageFilter>();
            composition.Register<IStatisticsQueryBuilder, StatisticsQueryBuilder>();
            composition.Register<IStoolballEntityCopier, StoolballEntityCopier>();
            composition.Register<IPlayerNameFormatter, PlayerNameFormatter>();
            composition.Register<IMatchInningsFactory, MatchInningsFactory>();
            composition.Register<Stoolball.Html.IHtmlSanitizer, Stoolball.Web.Html.HtmlSanitizer>();
            composition.Register<IUrlFormatter, UrlFormatter>();
            composition.Register<ISocialMediaAccountFormatter, SocialMediaAccountFormatter>();
            composition.Register<IMatchFilterQueryStringParser, MatchFilterQueryStringParser>();
            composition.Register<IStatisticsFilterQueryStringParser, StatisticsFilterQueryStringParser>();
            composition.Register<IMatchFilterHumanizer, MatchFilterHumanizer>();
            composition.Register<IStatisticsFilterHumanizer, StatisticsFilterHumanizer>();
            composition.Register<IStoolballEntityRouteParser, StoolballEntityRouteParser>();
            composition.Register<ICreateMemberExecuter, CreateMemberExecuter>();
            composition.Register<ILoginMemberWrapper, LoginMemberWrapper>();

            // Listings pages
            composition.Register<IListingsModelBuilder<TeamListing, TeamListingFilter, TeamsViewModel>, ListingsModelBuilder<TeamListing, TeamListingFilter, TeamsViewModel>>();
            composition.Register<IListingsModelBuilder<Competition, CompetitionFilter, CompetitionsViewModel>, ListingsModelBuilder<Competition, CompetitionFilter, CompetitionsViewModel>>();
            composition.Register<IListingsModelBuilder<MatchLocation, MatchLocationFilter, MatchLocationsViewModel>, ListingsModelBuilder<MatchLocation, MatchLocationFilter, MatchLocationsViewModel>>();
            composition.Register<IListingsModelBuilder<School, SchoolFilter, SchoolsViewModel>, ListingsModelBuilder<School, SchoolFilter, SchoolsViewModel>>();

            // Controllers for stoolball data pages. Register the concrete class since it'll never need to 
            // be injected anywhere except the one place where it's serving a page of content.
            composition.Register<IStoolballRouteTypeMapper, StoolballRouteTypeMapper>();
            composition.Register<IStoolballRouterController, StoolballRouterController>();

            // Caching with Polly
            composition.Register<IClearableCache, ClearableCacheWrapper>();
            composition.Register<ICacheClearer<Tournament>, TournamentCacheClearer>();
            composition.Register<ICacheClearer<Match>, MatchCacheClearer>();

            // Data sources for stoolball data.
            composition.Register<IRedirectsRepository, SkybrudRedirectsRepository>();
            composition.Register<IClubDataSource, SqlServerClubDataSource>();
            composition.Register<IClubRepository, SqlServerClubRepository>();
            composition.Register<ITeamListingDataSource, CachedTeamListingDataSource>();
            composition.Register<ICacheableTeamListingDataSource, SqlServerTeamListingDataSource>();
            composition.Register<ITeamRepository, SqlServerTeamRepository>();
            composition.Register<IPlayerRepository, SqlServerPlayerRepository>();
            composition.Register<IMatchLocationRepository, SqlServerMatchLocationRepository>();
            composition.Register<ICompetitionDataSource, CachedCompetitionDataSource>();
            composition.Register<ICacheableCompetitionDataSource, SqlServerCompetitionDataSource>();
            composition.Register<ICompetitionRepository, SqlServerCompetitionRepository>();
            composition.Register<ISeasonRepository, SqlServerSeasonRepository>();
            composition.Register<IMatchDataSource, SqlServerMatchDataSource>();
            composition.Register<IMatchListingDataSource, CachedMatchListingDataSource>();
            composition.Register<ICacheableMatchListingDataSource, SqlServerMatchListingDataSource>();
            composition.Register<ICommentsDataSource<Match>, CachedCommentsDataSource<Match>>();
            composition.Register<ICommentsDataSource<Tournament>, CachedCommentsDataSource<Tournament>>();
            composition.Register<ICacheableCommentsDataSource<Match>, SqlServerMatchCommentsDataSource>();
            composition.Register<ICacheableCommentsDataSource<Tournament>, SqlServerTournamentCommentsDataSource>();
            composition.Register<IMatchRepository, SqlServerMatchRepository>();
            composition.Register<ITournamentDataSource, SqlServerTournamentDataSource>();
            composition.Register<ITournamentRepository, SqlServerTournamentRepository>();
            composition.Register<IBestPerformanceInAMatchStatisticsDataSource, CachedBestPerformanceInAMatchStatisticsDataSource>();
            composition.Register<ICacheableBestPerformanceInAMatchStatisticsDataSource, SqlServerBestPerformanceInAMatchStatisticsDataSource>();
            composition.Register<IStatisticsRepository, SqlServerStatisticsRepository>();
            composition.Register<IInningsStatisticsDataSource, CachedInningsStatisticsDataSource>();
            composition.Register<ICacheableInningsStatisticsDataSource, SqlServerInningsStatisticsDataSource>();
            composition.Register<IMatchFilterFactory, MatchFilterFactory>();
            composition.Register<IPlayerSummaryStatisticsDataSource, CachedPlayerSummaryStatisticsDataSource>();
            composition.Register<ICacheablePlayerSummaryStatisticsDataSource, SqlServerPlayerSummaryStatisticsDataSource>();
            composition.Register<IPlayerPerformanceStatisticsDataSource, CachedPlayerPerformanceStatisticsDataSource>();
            composition.Register<ICacheablePlayerPerformanceStatisticsDataSource, SqlServerPlayerPerformanceStatisticsDataSource>();
            composition.Register<IBestPlayerTotalStatisticsDataSource, CachedBestPlayerTotalStatisticsDataSource>();
            composition.Register<ICacheableBestPlayerTotalStatisticsDataSource, SqlServerBestPlayerTotalStatisticsDataSource>();
            composition.Register<IBestPlayerAverageStatisticsDataSource, CachedBestPlayerAverageStatisticsDataSource>();
            composition.Register<ICacheableBestPlayerAverageStatisticsDataSource, SqlServerBestPlayerAverageStatisticsDataSource>();
            composition.Register<ISchoolDataSource, SqlServerSchoolDataSource>();

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