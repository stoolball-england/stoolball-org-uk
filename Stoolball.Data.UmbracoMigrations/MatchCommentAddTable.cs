using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording comments on stoolball matches 
    /// </summary>
    public partial class MatchCommentAddTable : MigrationBase
    {
        public MatchCommentAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<MatchCommentAddTable>("Running migration {MigrationStep}", typeof(MatchCommentAddTable).Name);

            if (TableExists(Constants.Tables.MatchComment) == false)
            {
                Create.Table<MatchCommentTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<MatchCommentAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.MatchComment);
            }
        }
    }
}