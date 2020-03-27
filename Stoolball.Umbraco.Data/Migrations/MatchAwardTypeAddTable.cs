using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Data.Migrations
{
    /// <summary>
    /// Adds a table for recording the types of awards for performances in stoolball matches and tournaments
    /// </summary>
    public partial class MatchAwardTypeAddTable : MigrationBase
    {
        public MatchAwardTypeAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<MatchAwardTypeAddTable>("Running migration {MigrationStep}", typeof(MatchAwardTypeAddTable).Name);

            if (TableExists(Constants.Tables.MatchAwardType) == false)
            {
                Create.Table<MatchAwardTypeTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<MatchAwardTypeAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.MatchAwardType);
            }
        }
    }
}