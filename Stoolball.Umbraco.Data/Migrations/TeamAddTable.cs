using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Data.Migrations
{
    /// <summary>
    /// Adds a table for recording stoolball teams
    /// </summary>
    public partial class TeamAddTable : MigrationBase
    {
        public TeamAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<TeamAddTable>("Running migration {MigrationStep}", typeof(TeamAddTable).Name);

            if (TableExists(Constants.Tables.Team) == false)
            {
                Create.Table<TeamTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<TeamAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.Team);
            }
        }
    }
}