using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the names of stoolball competitions which can change over time
    /// </summary>
    public partial class CompetitionVersionAddTable : MigrationBase
    {
        public CompetitionVersionAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<CompetitionVersionAddTable>("Running migration {MigrationStep}", typeof(CompetitionVersionAddTable).Name);

            if (TableExists(Tables.CompetitionVersion) == false)
            {
                Create.Table<CompetitionVersionTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<CompetitionVersionAddTable>("The database table {DbTable} already exists, skipping", Tables.CompetitionVersion);
            }
        }
    }
}