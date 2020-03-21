using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Data
{
    /// <summary>
    /// Adds a table for recording stoolball seasons
    /// </summary>
    public partial class SeasonAddTable : MigrationBase
    {
        public SeasonAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<SeasonAddTable>("Running migration {MigrationStep}", typeof(SeasonAddTable).Name);

            if (TableExists(Constants.Tables.Season) == false)
            {
                Create.Table<SeasonTableSchema>().Do();
            }
            else
            {
                Logger.Debug<SeasonAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.Season);
            }
        }
    }
}