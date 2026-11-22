using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the fall of wickets during an innings of a match
    /// </summary>
    public partial class FallOfWicketAddTable : AsyncMigrationBase
    {
        public FallOfWicketAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(FallOfWicketAddTable).Name);

            if (TableExists(Tables.FallOfWicket) == false)
            {
                Create.Table<FallOfWicketTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.FallOfWicket);
            }

            return Task.CompletedTask;
        }
    }
}