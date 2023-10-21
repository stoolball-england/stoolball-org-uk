using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a Deleted column to StoolballPlayerIdentity which is used to track identities which are obsolete, and can be deleted once statistics are up-to-date.
    /// Updates usp_Player_Async_Update to carry out that delete.
    /// </summary>
    public partial class DeleteObsoletePlayers : MigrationBase
    {
        public DeleteObsoletePlayers(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(DeleteObsoletePlayers).Name);

            if (!ColumnExists(Tables.PlayerIdentity, "Deleted"))
            {
                Create.Column("Deleted").OnTable(Tables.PlayerIdentity).AsBoolean().WithDefaultValue(0).Do();
                Execute.SqlFromFile("043_Delete_Obsolete_Players.usp_Player_Async_Update.sql").Do();
                Execute.SqlFromFile("043_Delete_Obsolete_Players.Clean_Existing_Data.sql").Do();
            }
            else
            {
                Logger.LogDebug("The database column {DbTable}.{DbColumn} already exists, skipping", Tables.PlayerIdentity, "Deleted");
            }
        }
    }
}