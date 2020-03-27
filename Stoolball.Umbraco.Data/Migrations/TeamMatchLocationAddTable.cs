using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Data.Migrations
{
    /// <summary>
    /// Adds a table for recording the home grounds and venues of stoolball teams which can change over time
    /// </summary>
    public partial class TeamMatchLocationAddTable : MigrationBase
    {
        public TeamMatchLocationAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<TeamMatchLocationAddTable>("Running migration {MigrationStep}", typeof(TeamMatchLocationAddTable).Name);

            if (TableExists(Constants.Tables.TeamMatchLocation) == false)
            {
                Create.Table<TeamMatchLocationTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<TeamMatchLocationAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.TeamMatchLocation);
            }
        }
    }
}