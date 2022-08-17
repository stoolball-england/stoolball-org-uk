using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

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

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(TournamentSeasonAddTable).Name);

            if (TableExists(Tables.TournamentSeason) == false)
            {
                Create.Table<TournamentSeasonTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.TournamentSeason);
            }
        }
    }
}