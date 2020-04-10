using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Clubs;
using Stoolball.Umbraco.Data.Redirects;
using Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators;
using Stoolball.Web.Clubs;
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
            composition.Register<IVerificationToken, VerificationToken>(Lifetime.Singleton);
            composition.Register<IAuditRepository, SqlServerAuditRepository>(Lifetime.Singleton);
            composition.Register<IRouteNormaliser, RouteNormaliser>(Lifetime.Singleton);

            // Data migration from the old Stoolball England website
            composition.Register<IRedirectsDataMigrator, SkybrudRedirectsDataMigrator>(Lifetime.Singleton);
            composition.Register<IClubDataMigrator, SqlServerClubDataMigrator>(Lifetime.Singleton);
            composition.Register<ISchoolDataMigrator, SqlServerSchoolDataMigrator>(Lifetime.Singleton);
            composition.Register<IMatchLocationDataMigrator, SqlServerMatchLocationDataMigrator>(Lifetime.Singleton);
            composition.Register<ITeamDataMigrator, SqlServerTeamDataMigrator>(Lifetime.Singleton);

            // Controllers for stoolball data pages. Register the concrete class since it'll never need to 
            // be injected anywhere except the one place where it's serving a page of content.
            composition.Register<ClubController>(Lifetime.Request);

            // Data sources for stoolball data.
            composition.Register<IRedirectsRepository, SkybrudRedirectsRepository>(Lifetime.Singleton);
            composition.Register<IClubDataSource, SqlServerClubDataSource>(Lifetime.Singleton);
        }
    }
}