using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the teams playing in a stoolball match
    /// </summary>
    public partial class MatchTeamAddTable : AsyncMigrationBase
    {
        public MatchTeamAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(MatchTeamAddTable).Name);

            if (TableExists(Tables.MatchTeam) == false)
            {
                Create.Table<MatchTeamTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.MatchTeam);
            }

            return Task.CompletedTask;
        }
    }
}