using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording awards for performances in stoolball matches, tournaments and seasons
    /// </summary>
    public partial class AwardedToAddTable : MigrationBase
    {
        public AwardedToAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<AwardedToAddTable>("Running migration {MigrationStep}", typeof(AwardedToAddTable).Name);

            if (TableExists(Tables.AwardedTo) == false)
            {
                Create.Table<AwardedToTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<AwardedToAddTable>("The database table {DbTable} already exists, skipping", Tables.AwardedTo);
            }
        }
    }
}