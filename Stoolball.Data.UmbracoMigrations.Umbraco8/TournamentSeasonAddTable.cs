using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the seasons a tournament is listed in
    /// </summary>
    public partial class TournamentSeasonAddTable : MigrationBase
    {
        public TournamentSeasonAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<TournamentSeasonAddTable>("Running migration {MigrationStep}", typeof(TournamentSeasonAddTable).Name);

            if (TableExists(Tables.TournamentSeason) == false)
            {
                Create.Table<TournamentSeasonTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<TournamentSeasonAddTable>("The database table {DbTable} already exists, skipping", Tables.TournamentSeason);
            }
        }
    }
}