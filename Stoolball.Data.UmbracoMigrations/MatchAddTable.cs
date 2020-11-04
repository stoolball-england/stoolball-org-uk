using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording stoolball matches 
    /// </summary>
    public partial class MatchAddTable : MigrationBase
    {
        public MatchAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<MatchAddTable>("Running migration {MigrationStep}", typeof(MatchAddTable).Name);

            if (TableExists(Constants.Tables.Match) == false)
            {
                Create.Table<MatchTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<MatchAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.Match);
            }
        }
    }
}