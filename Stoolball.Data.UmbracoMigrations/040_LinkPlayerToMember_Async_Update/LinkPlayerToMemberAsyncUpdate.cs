using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a column marking a player for deletion after linking a player to a member account.
    /// Adds a stored procedure to complete the process of linking/unlinking a player to a member account asynchronously.
    /// Adds an index to the StoolballPlayerInMatchStatistics.PlayerRoute column so that the query in the stored procedure does not need a table scan.
    /// </summary>
    public partial class LinkPlayerToMemberAsyncUpdate : MigrationBase
    {
        public LinkPlayerToMemberAsyncUpdate(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(LinkPlayerToMemberAsyncUpdate).Name);

            if (!ColumnExists(Tables.Player, "ForAsyncDelete"))
            {
                Create.Column("ForAsyncDelete").OnTable(Tables.Player).AsBoolean().WithDefaultValue(0).Do();
                Execute.SqlFromFile("040_LinkPlayerToMember_Async_Update.usp_Link_Player_To_Member_Async_Update.sql").Do();
                Execute.SqlFromFile("040_LinkPlayerToMember_Async_Update.IX_StoolballPlayerInMatchStatistics_PlayerRoute.sql").Do();
            }
            else
            {
                Logger.LogDebug("The database column {DbTable}.{DbColumn} already exists, skipping", Tables.Player, "ForAsyncDelete");
            }
        }
    }
}