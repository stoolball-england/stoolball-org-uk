using Ganss.XSS;
using Stoolball.Clubs;
using Stoolball.Competitions;
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
            composition.Register<ClubController>();
            composition.Register<ClubActionsController>();
            composition.Register<CreateClubController>();
            composition.Register<EditClubController>();
            composition.Register<DeleteClubController>();
            composition.Register<TeamsController>();
            composition.Register<TeamController>();
            composition.Register<TeamActionsController>();
            composition.Register<CreateTeamController>();
            composition.Register<EditTeamController>();
            composition.Register<DeleteTeamController>();
            composition.Register<TransientTeamController>();
            composition.Register<MatchesForClubController>();
            composition.Register<MatchesForTeamController>();
            composition.Register<MatchesForMatchLocationController>();
            composition.Register<MatchesForSeasonController>();
            composition.Register<MatchLocationsController>();
            composition.Register<MatchLocationController>();
            composition.Register<MatchLocationActionsController>();
            composition.Register<CreateMatchLocationController>();
            composition.Register<EditMatchLocationController>();
            composition.Register<DeleteMatchLocationController>();
            composition.Register<MatchController>();
            composition.Register<MatchActionsController>();
            composition.Register<CreateFriendlyMatchController>();
            composition.Register<CreateKnockoutMatchController>();
            composition.Register<CreateLeagueMatchController>();
            composition.Register<EditFriendlyMatchController>();
            composition.Register<EditLeagueMatchController>();
            composition.Register<EditKnockoutMatchController>();
            composition.Register<EditStartOfPlayController>();
            composition.Register<EditBowlingScorecardController>();
            composition.Register<EditBattingScorecardController>();
            composition.Register<EditCloseOfPlayController>();
            composition.Register<DeleteMatchController>();
            composition.Register<TournamentController>();
            composition.Register<TournamentActionsController>();
            composition.Register<CreateTournamentController>();
            composition.Register<EditTournamentController>();
            composition.Register<EditTournamentTeamsController>();
            composition.Register<DeleteTournamentController>();
            composition.Register<SeasonController>();
            composition.Register<SeasonResultsTableController>();
            composition.Register<SeasonActionsController>();
            composition.Register<CreateSeasonController>();
            composition.Register<EditSeasonController>();
            composition.Register<EditSeasonResultsTableController>();
            composition.Register<EditSeasonTeamsController>();
            composition.Register<DeleteSeasonController>();
            composition.Register<CompetitionsController>();
            composition.Register<CompetitionController>();
            composition.Register<CompetitionActionsController>();
            composition.Register<CreateCompetitionController>();
            composition.Register<EditCompetitionController>();
            composition.Register<DeleteCompetitionController>();
            composition.Register<StatisticsController>();
            composition.Register<EditStatisticsController>();
            composition.Register<IndividualScoresController>();

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
            composition.Register<IMatchListingDataSource, SqlServerMatchListingDataSource>();
            composition.Register<ICommentsDataSource<Match>, SqlServerMatchCommentsDataSource>();
            composition.Register<ICommentsDataSource<Tournament>, SqlServerTournamentCommentsDataSource>();
            composition.Register<IMatchRepository, SqlServerMatchRepository>();
            composition.Register<ITournamentDataSource, SqlServerTournamentDataSource>();
            composition.Register<ITournamentRepository, SqlServerTournamentRepository>();
            composition.Register<IStatisticsDataSource, SqlServerStatisticsDataSource>();
            composition.Register<IStatisticsRepository, SqlServerStatisticsRepository>();

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