using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Clubs;
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
            composition.Register<IClubDataMigrator, SqlServerClubDataMigrator>(Lifetime.Singleton);
            composition.Register<IRedirectsDataMigrator, SkybrudRedirectsDataMigrator>(Lifetime.Singleton);

            // Controllers for stoolball data pages. Register the concrete class since it'll never need to 
            // be injected anywhere except the one place where it's serving a page of content.
            composition.Register<ClubController>(Lifetime.Request);

            // Data sources for stoolball data.
            composition.Register<IClubDataSource, SqlServerClubDataSource>(Lifetime.Singleton);
        }
    }
}