using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording awards for performances in stoolball seasons
    /// </summary>
    public partial class SeasonAwardAddTable : MigrationBase
    {
        public SeasonAwardAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<SeasonAwardAddTable>("Running migration {MigrationStep}", typeof(SeasonAwardAddTable).Name);

            if (TableExists(Constants.Tables.SeasonAward) == false)
            {
                Create.Table<SeasonAwardTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<SeasonAwardAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.SeasonAward);
            }
        }
    }
}