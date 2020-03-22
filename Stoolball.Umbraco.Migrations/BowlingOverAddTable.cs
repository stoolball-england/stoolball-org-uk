using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Data
{
    /// <summary>
    /// Adds a table for recording overs bowled in stoolball matches
    /// </summary>
    public partial class BowlingOverAddTable : MigrationBase
    {
        public BowlingOverAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<BowlingOverAddTable>("Running migration {MigrationStep}", typeof(BowlingOverAddTable).Name);

            if (TableExists(Constants.Tables.BowlingOver) == false)
            {
                Create.Table<BowlingOverTableSchema>().Do();
            }
            else
            {
                Logger.Debug<BowlingOverAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.BowlingOver);
            }
        }
    }
}