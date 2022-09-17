using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a column for linking a player to a member account
    /// </summary>
    public partial class LinkPlayerToMember : MigrationBase
    {
        public LinkPlayerToMember(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(LinkPlayerToMember).Name);

            if (!ColumnExists(Tables.Player, "MemberKey"))
            {
                Create.Column("MemberKey").OnTable(Tables.Player).AsGuid().Nullable().ForeignKey("umbracoNode", "uniqueId").Do();
            }
            else
            {
                Logger.LogDebug("The database column {DbTable}.{DbColumn} already exists, skipping", Tables.Player, "MemberKey");
            }
        }
    }
}