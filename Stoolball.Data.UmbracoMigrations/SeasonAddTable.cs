using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
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

            if (TableExists(Tables.Season) == false)
            {
                Create.Table<SeasonTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<SeasonAddTable>("The database table {DbTable} already exists, skipping", Tables.Season);
            }
        }
    }
}