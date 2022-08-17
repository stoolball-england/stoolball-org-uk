using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the match types possible in stoolball seasons
    /// </summary>
    public partial class SeasonMatchTypeAddTable : MigrationBase
    {
        public SeasonMatchTypeAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(SeasonMatchTypeAddTable).Name);

            if (TableExists(Tables.SeasonMatchType) == false)
            {
                Create.Table<SeasonMatchTypeTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.SeasonMatchType);
            }
        }
    }
}