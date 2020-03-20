using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Data
{
    /// <summary>
    /// Adds a table for recording changes made to stoolball data, particularly by members
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
                Create.Table<AuditTableSchema>().Do();
            }
            else
            {
                Logger.Debug<AuditAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.Audit);
            }
        }
    }
}