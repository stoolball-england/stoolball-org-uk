using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording teams playing in stoolball seasons
    /// </summary>
    public partial class SeasonTeamAddTable : MigrationBase
    {
        public SeasonTeamAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(SeasonTeamAddTable).Name);

            if (TableExists(Tables.SeasonTeam) == false)
            {
                Create.Table<SeasonTeamTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.SeasonTeam);
            }
        }
    }
}