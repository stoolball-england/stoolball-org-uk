using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Email;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Umbraco.Data;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Clubs;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Umbraco.Data.MatchLocations;
using Stoolball.Umbraco.Data.Redirects;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators;
using Stoolball.Web.Clubs;
using Stoolball.Web.Competitions;
using Stoolball.Web.Configuration;
using Stoolball.Web.Matches;
using Stoolball.Web.MatchLocations;
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
            composition.Register<IRouteNormaliser, RouteNormaliser>(Lifetime.Singleton);
            composition.Register<IApiKeyProvider, ConfigApiKeyProvider>(Lifetime.Singleton);
            composition.Register<IDateTimeFormatter, DateTimeFormatter>(Lifetime.Singleton);
            composition.Register<IEstimatedSeason, EstimatedSeason>(Lifetime.Singleton);

            // Data migration from the old Stoolball England website
            composition.Register<IAuditHistoryBuilder, AuditHistoryBuilder>(Lifetime.Singleton);
            composition.Register<IRedirectsDataMigrator, SkybrudRedirectsDataMigrator>(Lifetime.Singleton);
            composition.Register<IClubDataMigrator, SqlServerClubDataMigrator>(Lifetime.Singleton);
            composition.Register<ISchoolDataMigrator, SqlServerSchoolDataMigrator>(Lifetime.Singleton);
            composition.Register<IMatchLocationDataMigrator, SqlServerMatchLocationDataMigrator>(Lifetime.Singleton);
            composition.Register<ITeamDataMigrator, SqlServerTeamDataMigrator>(Lifetime.Singleton);
            composition.Register<ICompetitionDataMigrator, SqlServerCompetitionDataMigrator>(Lifetime.Singleton);
            composition.Register<IMatchDataMigrator, SqlServerMatchDataMigrator>(Lifetime.Singleton);
            composition.Register<IPlayerDataMigrator, SqlServerPlayerDataMigrator>(Lifetime.Singleton);
            composition.Register<IPlayerPerformanceDataMigrator, SqlServerPlayerPerformanceDataMigrator>(Lifetime.Singleton);

            // Controllers for stoolball data pages. Register the concrete class since it'll never need to 
            // be injected anywhere except the one place where it's serving a page of content.
            composition.Register<ClubsController>(Lifetime.Request);
            composition.Register<ClubController>(Lifetime.Request);
            composition.Register<TeamsController>(Lifetime.Request);
            composition.Register<TeamController>(Lifetime.Request);
            composition.Register<TransientTeamController>(Lifetime.Request);
            composition.Register<MatchesForClubController>(Lifetime.Request);
            composition.Register<MatchesForTeamController>(Lifetime.Request);
            composition.Register<MatchesForMatchLocationController>(Lifetime.Request);
            composition.Register<MatchesForSeasonController>(Lifetime.Request);
            composition.Register<MatchLocationsController>(Lifetime.Request);
            composition.Register<MatchLocationController>(Lifetime.Request);
            composition.Register<MatchController>(Lifetime.Request);
            composition.Register<TournamentController>(Lifetime.Request);
            composition.Register<SeasonController>(Lifetime.Request);
            composition.Register<CompetitionController>(Lifetime.Request);

            // Data sources for stoolball data.
            composition.Register<IDatabaseConnectionFactory, UmbracoDatabaseConnectionFactory>(Lifetime.Singleton);
            composition.Register<IRedirectsRepository, SkybrudRedirectsRepository>(Lifetime.Singleton);
            composition.Register<IClubDataSource, SqlServerClubDataSource>(Lifetime.Singleton);
            composition.Register<ITeamDataSource, SqlServerTeamDataSource>(Lifetime.Singleton);
            composition.Register<IMatchLocationDataSource, SqlServerMatchLocationDataSource>(Lifetime.Singleton);
            composition.Register<ISeasonDataSource, SqlServerSeasonDataSource>(Lifetime.Singleton);
            composition.Register<IMatchDataSource, SqlServerMatchDataSource>(Lifetime.Singleton);
            composition.Register<ITournamentDataSource, SqlServerTournamentDataSource>(Lifetime.Singleton);
        }
    }
}