using Stoolball.Security;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators;
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace Stoolball.Web
{
    public class DependencyInjectionComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Register<Email.IEmailFormatter, Email.EmailFormatter>(Lifetime.Singleton);
            composition.Register<Email.IEmailSender, Email.EmailSender>(Lifetime.Singleton);
            composition.Register<IVerificationToken, VerificationToken>(Lifetime.Singleton);
            composition.Register<IAuditRepository, SqlServerAuditRepository>(Lifetime.Singleton);
            composition.Register<IClubDataMigrator, SqlServerClubDataMigrator>(Lifetime.Singleton);
        }
    }
}