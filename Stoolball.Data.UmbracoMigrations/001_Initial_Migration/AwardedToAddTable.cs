using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording awards for performances in stoolball matches, tournaments and seasons
    /// </summary>
    public partial class AwardedToAddTable : MigrationBase
    {
        public AwardedToAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(AwardedToAddTable).Name);

            if (TableExists(Tables.AwardedTo) == false)
            {
                Create.Table<AwardedToTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.AwardedTo);
            }
        }
    }
}