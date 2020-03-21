using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Data
{
    /// <summary>
    /// Adds a table for recording stoolball matches and tournaments
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
                Create.Table<MatchTableSchema>().Do();
            }
            else
            {
                Logger.Debug<MatchAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.Match);
            }
        }
    }
}