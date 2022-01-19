using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording comments on stoolball matches and tournaments
    /// </summary>
    public partial class CommentAddTable : MigrationBase
    {
        public CommentAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(CommentAddTable).Name);

            if (TableExists(Tables.Comment) == false)
            {
                Create.Table<CommentTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.Comment);
            }
        }
    }
}