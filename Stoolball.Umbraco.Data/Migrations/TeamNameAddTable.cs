using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Data.Migrations
{
    /// <summary>
    /// Adds a table for recording the names of stoolball teams which can change over time
    /// </summary>
    public partial class TeamNameAddTable : MigrationBase
    {
        public TeamNameAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<TeamNameAddTable>("Running migration {MigrationStep}", typeof(TeamNameAddTable).Name);

            if (TableExists(Constants.Tables.TeamName) == false)
            {
                Create.Table<TeamNameTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<TeamNameAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.TeamName);
            }
        }
    }
}