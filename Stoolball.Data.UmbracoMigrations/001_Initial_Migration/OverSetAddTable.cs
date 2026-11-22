using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the sets of overs that make up a match innings
    /// </summary>
    public partial class OverSetAddTable : AsyncMigrationBase
    {
        public OverSetAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(OverSetAddTable).Name);

            if (TableExists(Tables.OverSet) == false)
            {
                Create.Table<OverSetTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.OverSet);
            }

            return Task.CompletedTask;
        }
    }
}