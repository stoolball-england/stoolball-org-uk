using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording overs bowled in stoolball matches
    /// </summary>
    public partial class OverAddTable : MigrationBase
    {
        public OverAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<OverAddTable>("Running migration {MigrationStep}", typeof(OverAddTable).Name);

            if (TableExists(Constants.Tables.Over) == false)
            {
                Create.Table<OverTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<OverAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.Over);
            }
        }
    }
}