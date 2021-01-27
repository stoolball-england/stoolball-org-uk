using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

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

        public override void Migrate()
        {
            Logger.Debug<CommentAddTable>("Running migration {MigrationStep}", typeof(CommentAddTable).Name);

            if (TableExists(Constants.Tables.Comment) == false)
            {
                Create.Table<CommentTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<CommentAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.Comment);
            }
        }
    }
}