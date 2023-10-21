using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a LinkedBy column to StoolballPlayerIdentity which is used to track how a player identity was linked to its current player
    /// </summary>
    public partial class AddLinkedByToPlayerIdentity : MigrationBase
    {
        public AddLinkedByToPlayerIdentity(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(AddLinkedByToPlayerIdentity).Name);

            if (!ColumnExists(Tables.PlayerIdentity, "LinkedBy"))
            {
                Create.Column("LinkedBy").OnTable(Tables.PlayerIdentity).AsString(255).WithDefaultValue("DefaultIdentity").Do();
                Execute.SqlFromFile("044_Add_PlayerIdentity_LinkedBy.vw_Stoolball_PlayerIdentity.sql").Do();
                Execute.SqlFromFile("044_Add_PlayerIdentity_LinkedBy.StoolballPlayerIdentity_PopulateLinkedBy.sql").Do();
            }
            else
            {
                Logger.LogDebug("The database column {DbTable}.{DbColumn} already exists, skipping", Tables.PlayerIdentity, "LinkedBy");
            }
        }
    }
}