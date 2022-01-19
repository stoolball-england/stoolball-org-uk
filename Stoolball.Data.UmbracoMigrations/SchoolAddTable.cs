using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording schools that play stoolball
    /// </summary>
    public partial class SchoolAddTable : MigrationBase
    {
        public SchoolAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(SchoolAddTable).Name);

            if (TableExists(Tables.School) == false)
            {
                Create.Table<SchoolTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.School);
            }
        }
    }
}