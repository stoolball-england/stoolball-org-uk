using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording stoolball tournaments
    /// </summary>
    public partial class TournamentAddTable : AsyncMigrationBase
    {
        public TournamentAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(TournamentAddTable).Name);

            if (TableExists(Tables.Tournament) == false)
            {
                Create.Table<TournamentTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.Tournament);
            }

            return Task.CompletedTask;
        }
    }
}