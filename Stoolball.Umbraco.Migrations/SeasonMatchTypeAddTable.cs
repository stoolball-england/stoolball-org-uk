using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Migrations
{
    /// <summary>
    /// Adds a table for recording the match types possible in stoolball seasons
    /// </summary>
    public partial class SeasonMatchTypeAddTable : MigrationBase
    {
        public SeasonMatchTypeAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<SeasonMatchTypeAddTable>("Running migration {MigrationStep}", typeof(SeasonMatchTypeAddTable).Name);

            if (TableExists(Constants.Tables.SeasonMatchType) == false)
            {
                Create.Table<SeasonMatchTypeTableSchema>().Do();
            }
            else
            {
                Logger.Debug<SeasonMatchTypeAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.SeasonMatchType);
            }
        }
    }
}