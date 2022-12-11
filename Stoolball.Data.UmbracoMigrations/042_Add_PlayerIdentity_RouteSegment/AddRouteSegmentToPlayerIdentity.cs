using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a RouteSegment column to StoolballPlayerIdentity which is used for routes that edit a single identity rather than a player
    /// </summary>
    public partial class AddRouteSegmentToPlayerIdentity : MigrationBase
    {
        public AddRouteSegmentToPlayerIdentity(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(AddRouteSegmentToPlayerIdentity).Name);

            if (!ColumnExists(Tables.PlayerIdentity, "RouteSegment"))
            {
                Create.Column("RouteSegment").OnTable(Tables.PlayerIdentity).AsString(255).Nullable().Do();
                Execute.SqlFromFile("042_Add_PlayerIdentity_RouteSegment.StoolballPlayerIdentity_PopulateRouteSegment.sql").Do();
                Execute.SqlFromFile("042_Add_PlayerIdentity_RouteSegment.usp_Player_Async_Update.sql").Do();
            }
            else
            {
                Logger.LogDebug("The database column {DbTable}.{DbColumn} already exists, skipping", Tables.PlayerIdentity, "RouteSegment");
            }
        }
    }
}