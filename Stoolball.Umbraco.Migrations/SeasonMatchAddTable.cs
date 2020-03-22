using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Data
{
    /// <summary>
    /// Adds a table for recording the matches in a stoolball season
    /// </summary>
    public partial class SeasonMatchAddTable : MigrationBase
    {
        public SeasonMatchAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<SeasonMatchAddTable>("Running migration {MigrationStep}", typeof(SeasonMatchAddTable).Name);

            if (TableExists(Constants.Tables.SeasonMatch) == false)
            {
                Create.Table<SeasonMatchTableSchema>().Do();
            }
            else
            {
                Logger.Debug<SeasonMatchAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.SeasonMatch);
            }
        }
    }
}