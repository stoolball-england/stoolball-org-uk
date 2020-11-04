using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the players in teams, who might have different names over time
    /// </summary>
    public partial class PlayerIdentityAddTable : MigrationBase
    {
        public PlayerIdentityAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<PlayerIdentityAddTable>("Running migration {MigrationStep}", typeof(PlayerIdentityAddTable).Name);

            if (TableExists(Constants.Tables.PlayerIdentity) == false)
            {
                Create.Table<PlayerIdentityTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<PlayerIdentityAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.PlayerIdentity);
            }
        }
    }
}