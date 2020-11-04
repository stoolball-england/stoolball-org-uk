using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording schools that play stoolball
    /// </summary>
    public partial class SchoolAddTable : MigrationBase
    {
        public SchoolAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<SchoolAddTable>("Running migration {MigrationStep}", typeof(SchoolAddTable).Name);

            if (TableExists(Constants.Tables.School) == false)
            {
                Create.Table<SchoolTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<SchoolAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.School);
            }
        }
    }
}