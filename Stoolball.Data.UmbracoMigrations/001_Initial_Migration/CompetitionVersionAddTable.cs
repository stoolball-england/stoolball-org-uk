using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the names of stoolball competitions which can change over time
    /// </summary>
    public partial class CompetitionVersionAddTable : AsyncMigrationBase
    {
        public CompetitionVersionAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(CompetitionVersionAddTable).Name);

            if (TableExists(Tables.CompetitionVersion) == false)
            {
                Create.Table<CompetitionVersionTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.CompetitionVersion);
            }

            return Task.CompletedTask;
        }
    }
}