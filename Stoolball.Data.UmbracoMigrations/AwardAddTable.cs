using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the types of awards for performances in stoolball matches and tournaments
    /// </summary>
    public partial class AwardAddTable : MigrationBase
    {
        public AwardAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(AwardAddTable).Name);

            if (TableExists(Tables.Award) == false)
            {
                Create.Table<AwardTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.Award);
            }
        }
    }
}