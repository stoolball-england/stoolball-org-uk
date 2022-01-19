using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the home grounds and venues of stoolball teams which can change over time
    /// </summary>
    public partial class TeamMatchLocationAddTable : MigrationBase
    {
        public TeamMatchLocationAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(TeamMatchLocationAddTable).Name);

            if (TableExists(Tables.TeamMatchLocation) == false)
            {
                Create.Table<TeamMatchLocationTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.TeamMatchLocation);
            }
        }
    }
}