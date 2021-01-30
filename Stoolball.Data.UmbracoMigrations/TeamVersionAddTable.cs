using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the names of stoolball teams which can change over time
    /// </summary>
    public partial class TeamVersionAddTable : MigrationBase
    {
        public TeamVersionAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<TeamVersionAddTable>("Running migration {MigrationStep}", typeof(TeamVersionAddTable).Name);

            if (TableExists(Tables.TeamVersion) == false)
            {
                Create.Table<TeamVersionTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<TeamVersionAddTable>("The database table {DbTable} already exists, skipping", Tables.TeamVersion);
            }
        }
    }
}