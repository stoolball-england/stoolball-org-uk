using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording stoolball tournaments
    /// </summary>
    public partial class TournamentAddTable : MigrationBase
    {
        public TournamentAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<TournamentAddTable>("Running migration {MigrationStep}", typeof(TournamentAddTable).Name);

            if (TableExists(Constants.Tables.Tournament) == false)
            {
                Create.Table<TournamentTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<TournamentAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.Tournament);
            }
        }
    }
}