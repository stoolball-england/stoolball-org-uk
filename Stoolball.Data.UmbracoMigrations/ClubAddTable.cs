using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording stoolball clubs
    /// </summary>
    public partial class ClubAddTable : MigrationBase
    {
        public ClubAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<ClubAddTable>("Running migration {MigrationStep}", typeof(ClubAddTable).Name);

            if (TableExists(Tables.Club) == false)
            {
                Create.Table<ClubTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<ClubAddTable>("The database table {DbTable} already exists, skipping", Tables.Club);
            }
        }
    }
}