using Ganss.XSS;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Email;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Umbraco.Data;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Clubs;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Umbraco.Data.MatchLocations;
using Stoolball.Umbraco.Data.Redirects;
using Stoolball.Umbraco.Data.Security;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators;
using Stoolball.Web.Clubs;
using Stoolball.Web.Competitions;
using Stoolball.Web.Configuration;
using Stoolball.Web.Matches;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
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
            composition.Register<Email.IEmailFormatter, Email.EmailFormatter>(Lifetime.Singleton);
            composition.Register<Email.IEmailSender, Email.EmailSender>(Lifetime.Singleton);
            composition.Register<IEmailProtector, EmailProtector>(Lifetime.Singleton);
            composition.Register<IVerificationToken, VerificationToken>(Lifetime.Singleton);
            composition.Register<IAuditRepository, SqlServerAuditRepository>(Lifetime.Singleton);
            composition.Register<IRouteGenerator, RouteGenerator>(Lifetime.Singleton);
            composition.Register<IRouteNormaliser, RouteNormaliser>(Lifetime.Singleton);
            composition.Register<IApiKeyProvider, ConfigApiKeyProvider>(Lifetime.Singleton);
            composition.Register<IDateTimeFormatter, DateTimeFormatter>(Lifetime.Singleton);
            composition.Register<ISeasonEstimator, SeasonEstimator>(Lifetime.Singleton);
            composition.Register<IHtmlSanitizer, HtmlSanitizer>(Lifetime.Singleton);
            composition.Register<ICreateMatchSeasonSelector, CreateMatchSeasonSelector>(Lifetime.Singleton);
            composition.Register<IMatchNameBuilder, MatchNameBuilder>(Lifetime.Singleton);
            composition.Register<IPlayerTypeSelector, PlayerTypeSelector>(Lifetime.Singleton);
            composition.Register<IEditMatchHelper, EditMatchHelper>(Lifetime.Singleton);
            composition.Register<IMemberGroupHelper, MemberGroupHelper>(Lifetime.Singleton);
            composition.Register<IMatchResultEvaluator, MatchResultEvaluator>(Lifetime.Singleton);

            // Data migration from the old Stoolball England website
            composition.Register<IAuditHistoryBuilder, AuditHistoryBuilder>(Lifetime.Singleton);
            composition.Register<IRedirectsDataMigrator, SkybrudRedirectsDataMigrator>(Lifetime.Singleton);
            composition.Register<IClubDataMigrator, SqlServerClubDataMigrator>(Lifetime.Singleton);
            composition.Register<ISchoolDataMigrator, SqlServerSchoolDataMigrator>(Lifetime.Singleton);
            composition.Register<IMatchLocationDataMigrator, SqlServerMatchLocationDataMigrator>(Lifetime.Singleton);
            composition.Register<ITeamDataMigrator, SqlServerTeamDataMigrator>(Lifetime.Singleton);
            composition.Register<ICompetitionDataMigrator, SqlServerCompetitionDataMigrator>(Lifetime.Singleton);
            composition.Register<IMatchDataMigrator, SqlServerMatchDataMigrator>(Lifetime.Singleton);
            composition.Register<ITournamentDataMigrator, SqlServerTournamentDataMigrator>(Lifetime.Singleton);
            composition.Register<IPlayerDataMigrator, SqlServerPlayerDataMigrator>(Lifetime.Singleton);
            composition.Register<IPlayerPerformanceDataMigrator, SqlServerPlayerPerformanceDataMigrator>(Lifetime.Singleton);
            composition.Register<IMatchAwardDataMigrator, SqlServerMatchAwardDataMigrator>(Lifetime.Singleton);
            composition.Register<IMatchCommentDataMigrator, SqlServerMatchCommentDataMigrator>(Lifetime.Singleton);
            composition.Register<IMatchCommentSubscriptionDataMigrator, SqlServerMatchCommentSubscriptionDataMigrator>(Lifetime.Singleton);
            composition.Register<ICompetitionSubscriptionDataMigrator, SqlServerCompetitionSubscriptionDataMigrator>(Lifetime.Singleton);

            // Controllers for stoolball data pages. Register the concrete class since it'll never need to 
            // be injected anywhere except the one place where it's serving a page of content.
            composition.Register<IStoolballRouteTypeMapper, StoolballRouteTypeMapper>(Lifetime.Singleton);
            composition.Register<IStoolballRouterController, StoolballRouterController>(Lifetime.Request);
            composition.Register<ClubController>(Lifetime.Request);
            composition.Register<ClubActionsController>(Lifetime.Request);
            composition.Register<CreateClubController>(Lifetime.Request);
            composition.Register<EditClubController>(Lifetime.Request);
            composition.Register<DeleteClubController>(Lifetime.Request);
            composition.Register<TeamsController>(Lifetime.Request);
            composition.Register<TeamController>(Lifetime.Request);
            composition.Register<TeamActionsController>(Lifetime.Request);
            composition.Register<CreateTeamController>(Lifetime.Request);
            composition.Register<EditTeamController>(Lifetime.Request);
            composition.Register<DeleteTeamController>(Lifetime.Request);
            composition.Register<TransientTeamController>(Lifetime.Request);
            composition.Register<MatchesForClubController>(Lifetime.Request);
            composition.Register<MatchesForTeamController>(Lifetime.Request);
            composition.Register<MatchesForMatchLocationController>(Lifetime.Request);
            composition.Register<MatchesForSeasonController>(Lifetime.Request);
            composition.Register<MatchLocationsController>(Lifetime.Request);
            composition.Register<MatchLocationController>(Lifetime.Request);
            composition.Register<MatchLocationActionsController>(Lifetime.Request);
            composition.Register<CreateMatchLocationController>(Lifetime.Request);
            composition.Register<EditMatchLocationController>(Lifetime.Request);
            composition.Register<DeleteMatchLocationController>(Lifetime.Request);
            composition.Register<MatchController>(Lifetime.Request);
            composition.Register<MatchActionsController>(Lifetime.Request);
            composition.Register<CreateFriendlyMatchController>(Lifetime.Request);
            composition.Register<CreateKnockoutMatchController>(Lifetime.Request);
            composition.Register<CreateLeagueMatchController>(Lifetime.Request);
            composition.Register<EditFriendlyMatchController>(Lifetime.Request);
            composition.Register<EditLeagueMatchController>(Lifetime.Request);
            composition.Register<EditKnockoutMatchController>(Lifetime.Request);
            composition.Register<EditStartOfPlayController>(Lifetime.Request);
            composition.Register<EditCloseOfPlayController>(Lifetime.Request);
            composition.Register<DeleteMatchController>(Lifetime.Request);
            composition.Register<TournamentController>(Lifetime.Request);
            composition.Register<TournamentActionsController>(Lifetime.Request);
            composition.Register<CreateTournamentController>(Lifetime.Request);
            composition.Register<EditTournamentController>(Lifetime.Request);
            composition.Register<EditTournamentTeamsController>(Lifetime.Request);
            composition.Register<DeleteTournamentController>(Lifetime.Request);
            composition.Register<SeasonController>(Lifetime.Request);
            composition.Register<SeasonResultsController>(Lifetime.Request);
            composition.Register<SeasonActionsController>(Lifetime.Request);
            composition.Register<CreateSeasonController>(Lifetime.Request);
            composition.Register<EditSeasonController>(Lifetime.Request);
            composition.Register<EditSeasonPointsController>(Lifetime.Request);
            composition.Register<EditSeasonTeamsController>(Lifetime.Request);
            composition.Register<DeleteSeasonController>(Lifetime.Request);
            composition.Register<CompetitionsController>(Lifetime.Request);
            composition.Register<CompetitionController>(Lifetime.Request);
            composition.Register<CompetitionActionsController>(Lifetime.Request);
            composition.Register<CreateCompetitionController>(Lifetime.Request);
            composition.Register<EditCompetitionController>(Lifetime.Request);
            composition.Register<DeleteCompetitionController>(Lifetime.Request);

            // Data sources for stoolball data.
            composition.Register<IDatabaseConnectionFactory, UmbracoDatabaseConnectionFactory>(Lifetime.Singleton);
            composition.Register<IRedirectsRepository, SkybrudRedirectsRepository>(Lifetime.Singleton);
            composition.Register<IClubDataSource, SqlServerClubDataSource>(Lifetime.Singleton);
            composition.Register<IClubRepository, SqlServerClubRepository>(Lifetime.Singleton);
            composition.Register<ITeamDataSource, SqlServerTeamDataSource>(Lifetime.Singleton);
            composition.Register<ITeamListingDataSource, SqlServerTeamListingDataSource>(Lifetime.Singleton);
            composition.Register<ITeamRepository, SqlServerTeamRepository>(Lifetime.Singleton);
            composition.Register<IPlayerDataSource, SqlServerPlayerDataSource>(Lifetime.Singleton);
            composition.Register<IMatchLocationDataSource, SqlServerMatchLocationDataSource>(Lifetime.Singleton);
            composition.Register<IMatchLocationRepository, SqlServerMatchLocationRepository>(Lifetime.Singleton);
            composition.Register<ICompetitionDataSource, SqlServerCompetitionDataSource>(Lifetime.Singleton);
            composition.Register<ICompetitionRepository, SqlServerCompetitionRepository>(Lifetime.Singleton);
            composition.Register<ISeasonDataSource, SqlServerSeasonDataSource>(Lifetime.Singleton);
            composition.Register<ISeasonRepository, SqlServerSeasonRepository>(Lifetime.Singleton);
            composition.Register<IMatchDataSource, SqlServerMatchDataSource>(Lifetime.Singleton);
            composition.Register<IMatchListingDataSource, SqlServerMatchListingDataSource>(Lifetime.Singleton);
            composition.Register<ICommentsDataSource<Match>, SqlServerMatchCommentsDataSource>(Lifetime.Singleton);
            composition.Register<ICommentsDataSource<Tournament>, SqlServerTournamentCommentsDataSource>(Lifetime.Singleton);
            composition.Register<IMatchRepository, SqlServerMatchRepository>(Lifetime.Singleton);
            composition.Register<ITournamentDataSource, SqlServerTournamentDataSource>(Lifetime.Singleton);
            composition.Register<ITournamentRepository, SqlServerTournamentRepository>(Lifetime.Singleton);

            // Security checks
            composition.Register<IAuthorizationPolicy<Club>, ClubAuthorizationPolicy>(Lifetime.Singleton);
            composition.Register<IAuthorizationPolicy<Competition>, CompetitionAuthorizationPolicy>(Lifetime.Singleton);
            composition.Register<IAuthorizationPolicy<MatchLocation>, MatchLocationAuthorizationPolicy>(Lifetime.Singleton);
            composition.Register<IAuthorizationPolicy<Match>, MatchAuthorizationPolicy>(Lifetime.Singleton);
            composition.Register<IAuthorizationPolicy<Tournament>, TournamentAuthorizationPolicy>(Lifetime.Singleton);
            composition.Register<IAuthorizationPolicy<Team>, TeamAuthorizationPolicy>(Lifetime.Singleton);
        }
    }
}