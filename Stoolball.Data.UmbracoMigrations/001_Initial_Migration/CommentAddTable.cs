using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording comments on stoolball matches and tournaments
    /// </summary>
    public partial class CommentAddTable : AsyncMigrationBase
    {
        public CommentAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
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

            return Task.CompletedTask;
        }
    }
}