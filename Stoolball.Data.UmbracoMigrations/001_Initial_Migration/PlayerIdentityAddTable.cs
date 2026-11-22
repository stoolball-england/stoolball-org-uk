using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the players in teams, who might have different names over time
    /// </summary>
    public partial class PlayerIdentityAddTable : AsyncMigrationBase
    {
        public PlayerIdentityAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(PlayerIdentityAddTable).Name);

            if (TableExists(Tables.PlayerIdentity) == false)
            {
                Create.Table<PlayerIdentityTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.PlayerIdentity);
            }

            return Task.CompletedTask;
        }
    }
}