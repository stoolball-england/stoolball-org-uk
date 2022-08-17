using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the names of schools that play stoolball, which change over time
    /// </summary>
    public partial class SchoolVersionAddTable : MigrationBase
    {
        public SchoolVersionAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(SchoolVersionAddTable).Name);

            if (TableExists(Tables.SchoolVersion) == false)
            {
                Create.Table<SchoolVersionTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.SchoolVersion);
            }
        }
    }
}