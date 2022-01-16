using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the names of stoolball clubs which may change over time
    /// </summary>
    public partial class ClubVersionAddTable : MigrationBase
    {
        public ClubVersionAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<ClubVersionAddTable>("Running migration {MigrationStep}", typeof(ClubVersionAddTable).Name);

            if (TableExists(Tables.ClubVersion) == false)
            {
                Create.Table<ClubVersionTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<ClubVersionAddTable>("The database table {DbTable} already exists, skipping", Tables.ClubVersion);
            }
        }
    }
}