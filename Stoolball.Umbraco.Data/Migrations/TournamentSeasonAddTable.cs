using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Data.Migrations
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

            if (TableExists(Constants.Tables.TournamentSeason) == false)
            {
                Create.Table<TournamentSeasonTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<TournamentSeasonAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.TournamentSeason);
            }
        }
    }
}