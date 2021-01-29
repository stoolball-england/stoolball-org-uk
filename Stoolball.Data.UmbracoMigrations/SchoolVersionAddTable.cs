using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the names of schools that play stoolball, which change over time
    /// </summary>
    public partial class SchoolVersionAddTable : MigrationBase
    {
        public SchoolVersionAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<SchoolVersionAddTable>("Running migration {MigrationStep}", typeof(SchoolVersionAddTable).Name);

            if (TableExists(Constants.Tables.SchoolVersion) == false)
            {
                Create.Table<SchoolVersionTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<SchoolVersionAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.SchoolVersion);
            }
        }
    }
}