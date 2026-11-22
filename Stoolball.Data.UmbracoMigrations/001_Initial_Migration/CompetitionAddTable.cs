using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording stoolball competitions
    /// </summary>
    public partial class CompetitionAddTable : AsyncMigrationBase
    {
        public CompetitionAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(CompetitionAddTable).Name);

            if (TableExists(Tables.Competition) == false)
            {
                Create.Table<CompetitionTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.Competition);
            }

            return Task.CompletedTask;
        }
    }
}