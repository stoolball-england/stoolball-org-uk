using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Data
{
    /// <summary>
    /// Adds a table for recording the names of schools that play stoolball, which change over time
    /// </summary>
    public partial class SchoolNameAddTable : MigrationBase
    {
        public SchoolNameAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<SchoolNameAddTable>("Running migration {MigrationStep}", typeof(SchoolNameAddTable).Name);

            if (TableExists(Constants.Tables.SchoolName) == false)
            {
                Create.Table<SchoolNameTableSchema>().Do();
            }
            else
            {
                Logger.Debug<SchoolNameAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.SchoolName);
            }
        }
    }
}