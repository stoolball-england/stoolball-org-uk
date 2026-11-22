using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording stoolball seasons
    /// </summary>
    public partial class SeasonAddTable : AsyncMigrationBase
    {
        public SeasonAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(SeasonAddTable).Name);

            if (TableExists(Tables.Season) == false)
            {
                Create.Table<SeasonTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.Season);
            }

            return Task.CompletedTask;
        }
    }
}