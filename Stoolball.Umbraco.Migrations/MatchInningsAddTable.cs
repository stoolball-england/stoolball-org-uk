using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Migrations
{
    /// <summary>
    /// Adds a table for recording team innings in stoolball matches
    /// </summary>
    public partial class MatchInningsAddTable : MigrationBase
    {
        public MatchInningsAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<MatchInningsAddTable>("Running migration {MigrationStep}", typeof(MatchInningsAddTable).Name);

            if (TableExists(Constants.Tables.MatchInnings) == false)
            {
                Create.Table<MatchInningsTableSchema>().Do();
            }
            else
            {
                Logger.Debug<MatchInningsAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.MatchInnings);
            }
        }
    }
}