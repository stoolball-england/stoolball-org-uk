using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the teams playing in a stoolball tournament
    /// </summary>
    public partial class TournamentTeamAddTable : AsyncMigrationBase
    {
        public TournamentTeamAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(TournamentTeamAddTable).Name);

            if (TableExists(Tables.TournamentTeam) == false)
            {
                Create.Table<TournamentTeamTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.TournamentTeam);
            }

            return Task.CompletedTask;
        }
    }
}