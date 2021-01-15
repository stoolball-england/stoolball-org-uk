using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording awards for performances in stoolball tournaments
    /// </summary>
    public partial class TournamentAwardAddTable : MigrationBase
    {
        public TournamentAwardAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<TournamentAwardAddTable>("Running migration {MigrationStep}", typeof(TournamentAwardAddTable).Name);

            if (TableExists(Constants.Tables.TournamentAward) == false)
            {
                Create.Table<TournamentAwardTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<TournamentAwardAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.TournamentAward);
            }
        }
    }
}