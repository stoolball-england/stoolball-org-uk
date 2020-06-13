using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Data.Migrations
{
    /// <summary>
    /// Adds a table for recording comments on stoolball matches
    /// </summary>
    public partial class TournamentCommentAddTable : MigrationBase
    {
        public TournamentCommentAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<TournamentCommentAddTable>("Running migration {MigrationStep}", typeof(TournamentCommentAddTable).Name);

            if (TableExists(Constants.Tables.TournamentComment) == false)
            {
                Create.Table<TournamentCommentTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<TournamentCommentAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.TournamentComment);
            }
        }
    }
}