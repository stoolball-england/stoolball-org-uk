using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording stoolball matches 
    /// </summary>
    public partial class MatchAddTable : AsyncMigrationBase
    {
        public MatchAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(MatchAddTable).Name);

            if (TableExists(Tables.Match) == false)
            {
                Create.Table<MatchTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.Match);
            }

            return Task.CompletedTask;
        }
    }
}