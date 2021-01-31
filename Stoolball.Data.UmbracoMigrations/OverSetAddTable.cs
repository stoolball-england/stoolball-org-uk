using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the sets of overs that make up a match innings
    /// </summary>
    public partial class OverSetAddTable : MigrationBase
    {
        public OverSetAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<OverSetAddTable>("Running migration {MigrationStep}", typeof(OverSetAddTable).Name);

            if (TableExists(Tables.OverSet) == false)
            {
                Create.Table<OverSetTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<OverSetAddTable>("The database table {DbTable} already exists, skipping", Tables.OverSet);
            }
        }
    }
}