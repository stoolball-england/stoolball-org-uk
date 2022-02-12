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
using Stoolball.Web.Caching;
using Stoolball.Web.Competitions;
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
            composition.Register<IAuditRepository, SqlServerAuditRepository>();
            composition.Register<IRouteGenerator, RouteGenerator>();
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
            composition.Register<ITeamListingFilterSerializer, TeamListingFilterQueryStringSerializer>();
            composition.Register<ICompetitionFilterSerializer, CompetitionFilterQueryStringSerializer>();
            composition.Register<IBadLanguageFilter, BadLanguageFilter>();
            composition.Register<IStoolballEntityCopier, StoolballEntityCopier>();
            composition.Register<IPlayerNameFormatter, PlayerNameFormatter>();
            composition.Register<IMatchInningsFactory, MatchInningsFactory>();
            composition.Register<Stoolball.Html.IHtmlSanitizer, Stoolball.Web.Html.HtmlSanitizer>();
            composition.Register<IUrlFormatter, UrlFormatter>();
            composition.Register<ISocialMediaAccountFormatter, SocialMediaAccountFormatter>();
            composition.Register<IStoolballEntityRouteParser, StoolballEntityRouteParser>();

            // Listings pages
            composition.Register<IListingsModelBuilder<TeamListing, TeamListingFilter, TeamsViewModel>, ListingsModelBuilder<TeamListing, TeamListingFilter, TeamsViewModel>>();
            composition.Register<IListingsModelBuilder<Competition, CompetitionFilter, CompetitionsViewModel>, ListingsModelBuilder<Competition, CompetitionFilter, CompetitionsViewModel>>();
            composition.Register<IListingsModelBuilder<MatchLocation, MatchLocationFilter, MatchLocationsViewModel>, ListingsModelBuilder<MatchLocation, MatchLocationFilter, MatchLocationsViewModel>>();
            composition.Register<IListingsModelBuilder<School, SchoolFilter, SchoolsViewModel>, ListingsModelBuilder<School, SchoolFilter, SchoolsViewModel>>();

            // Caching with Polly
            composition.Register<IClearableCache, ClearableCacheWrapper>();
            composition.Register<ICacheClearer<Tournament>, TournamentCacheClearer>();
            composition.Register<ICacheClearer<Match>, MatchCacheClearer>();

            // Data sources for stoolball data.
            composition.Register<IRedirectsRepository, SkybrudRedirectsRepository>();
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
            composition.Register<ICommentsDataSource<Match>, CachedCommentsDataSource<Match>>();
            composition.Register<ICommentsDataSource<Tournament>, CachedCommentsDataSource<Tournament>>();
            composition.Register<ICacheableCommentsDataSource<Match>, SqlServerMatchCommentsDataSource>();
            composition.Register<ICacheableCommentsDataSource<Tournament>, SqlServerTournamentCommentsDataSource>();
            composition.Register<IMatchRepository, SqlServerMatchRepository>();
            composition.Register<ITournamentDataSource, SqlServerTournamentDataSource>();
            composition.Register<ITournamentRepository, SqlServerTournamentRepository>();
            composition.Register<IStatisticsRepository, SqlServerStatisticsRepository>();
            composition.Register<IPlayerSummaryStatisticsDataSource, CachedPlayerSummaryStatisticsDataSource>();
            composition.Register<ICacheablePlayerSummaryStatisticsDataSource, SqlServerPlayerSummaryStatisticsDataSource>();
            composition.Register<IPlayerPerformanceStatisticsDataSource, CachedPlayerPerformanceStatisticsDataSource>();
            composition.Register<ICacheablePlayerPerformanceStatisticsDataSource, SqlServerPlayerPerformanceStatisticsDataSource>();
            composition.Register<IBestPlayerAverageStatisticsDataSource, CachedBestPlayerAverageStatisticsDataSource>();
            composition.Register<ICacheableBestPlayerAverageStatisticsDataSource, SqlServerBestPlayerAverageStatisticsDataSource>();
            composition.Register<ISchoolDataSource, SqlServerSchoolDataSource>();

            // Security checks
            composition.Register<Stoolball.Web.Security.IAuthorizationPolicy<Competition>, CompetitionAuthorizationPolicy>();
            composition.Register<Stoolball.Web.Security.IAuthorizationPolicy<MatchLocation>, MatchLocationAuthorizationPolicy>();
            composition.Register<Stoolball.Web.Security.IAuthorizationPolicy<Match>, MatchAuthorizationPolicy>();
            composition.Register<Stoolball.Web.Security.IAuthorizationPolicy<Tournament>, TournamentAuthorizationPolicy>();
            composition.Register<Stoolball.Web.Security.IAuthorizationPolicy<Team>, TeamAuthorizationPolicy>();
        }
    }
}