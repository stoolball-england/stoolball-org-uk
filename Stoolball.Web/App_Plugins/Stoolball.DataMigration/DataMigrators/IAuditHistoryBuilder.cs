using Stoolball.Logging;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface IAuditHistoryBuilder
    {
        void BuildInitialAuditHistory<T>(T original, T migrated, string actor) where T : IAuditable;
    }
}