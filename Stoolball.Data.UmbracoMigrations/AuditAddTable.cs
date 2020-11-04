using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

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

        public override void Migrate()
        {
            Logger.Debug<AuditAddTable>("Running migration {MigrationStep}", typeof(AuditAddTable).Name);

            if (TableExists(Constants.Tables.Audit) == false)
            {
                Create.Table<AuditTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<AuditAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.Audit);
            }
        }
    }
}