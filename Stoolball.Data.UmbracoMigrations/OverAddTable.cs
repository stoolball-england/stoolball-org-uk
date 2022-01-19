using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording overs bowled in stoolball matches
    /// </summary>
    public partial class OverAddTable : MigrationBase
    {
        public OverAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(OverAddTable).Name);

            if (TableExists(Tables.Over) == false)
            {
                Create.Table<OverTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.Over);
            }
        }
    }
}