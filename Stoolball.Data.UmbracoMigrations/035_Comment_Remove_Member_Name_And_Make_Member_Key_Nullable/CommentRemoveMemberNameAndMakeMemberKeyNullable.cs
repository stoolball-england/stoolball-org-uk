using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Removes the MemberName field from the Comments table and make the MemberKey nullable, because it duplicates the Umbraco member data and a member can be deleted
    /// </summary>
    public partial class CommentRemoveMemberNameAndMakeMemberKeyNullable : MigrationBase
    {
        public CommentRemoveMemberNameAndMakeMemberKeyNullable(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(CommentRemoveMemberNameAndMakeMemberKeyNullable).Name);

            if (ColumnExists(Tables.Comment, nameof(CommentTableInitialSchema.MemberName)))
            {
                Delete.Column(nameof(CommentTableInitialSchema.MemberName)).FromTable(Tables.Comment).Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} has no {Column} column, skipping", Tables.Comment, nameof(CommentTableInitialSchema.MemberName));
            }

            if (ColumnExists(Tables.Comment, nameof(CommentTableInitialSchema.MemberKey)))
            {
                Alter.Table(Tables.Comment).AlterColumn(nameof(CommentTableInitialSchema.MemberKey)).AsGuid().Nullable().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} has no {Column} column, skipping", Tables.Comment, nameof(CommentTableInitialSchema.MemberKey));
            }
        }
    }
}