using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording changes to stoolball data
    /// </summary>
    public partial class AuditAddTable : MigrationBase
    {
        public AuditAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(AuditAddTable).Name);

            if (TableExists(Tables.Audit) == false)
            {
                Create.Table<AuditTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.Audit);
            }
        }
    }
}