using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Data
{
    /// <summary>
    /// Adds a table for recording awards for performances in stoolball matches and tournaments
    /// </summary>
    public partial class MatchAwardAddTable : MigrationBase
    {
        public MatchAwardAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<MatchAwardAddTable>("Running migration {MigrationStep}", typeof(MatchAwardAddTable).Name);

            if (TableExists(Constants.Tables.MatchAward) == false)
            {
                Create.Table<MatchAwardTableSchema>().Do();
            }
            else
            {
                Logger.Debug<MatchAwardAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.MatchAward);
            }
        }
    }
}