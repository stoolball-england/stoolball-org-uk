using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the names of stoolball teams which can change over time
    /// </summary>
    public partial class TeamVersionAddTable : AsyncMigrationBase
    {
        public TeamVersionAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(TeamVersionAddTable).Name);

            if (TableExists(Tables.TeamVersion) == false)
            {
                Create.Table<TeamVersionTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.TeamVersion);
            }

            return Task.CompletedTask;
        }
    }
}