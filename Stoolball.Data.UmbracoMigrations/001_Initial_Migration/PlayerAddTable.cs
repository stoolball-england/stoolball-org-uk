using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording players, who might play for different teams and have different names over time
    /// </summary>
    public partial class PlayerAddTable : MigrationBase
    {
        public PlayerAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(PlayerAddTable).Name);

            if (TableExists(Tables.Player) == false)
            {
                Create.Table<PlayerTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.Player);
            }
        }
    }
}