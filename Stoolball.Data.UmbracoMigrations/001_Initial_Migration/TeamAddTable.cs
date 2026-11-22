using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording stoolball teams
    /// </summary>
    public partial class TeamAddTable : AsyncMigrationBase
    {
        public TeamAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(TeamAddTable).Name);

            if (TableExists(Tables.Team) == false)
            {
                Create.Table<TeamTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.Team);
            }

            return Task.CompletedTask;
        }
    }
}