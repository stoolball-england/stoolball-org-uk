using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Data.Migrations
{
    /// <summary>
    /// Adds a table for recording the teams playing in a stoolball match
    /// </summary>
    public partial class MatchTeamAddTable : MigrationBase
    {
        public MatchTeamAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<MatchTeamAddTable>("Running migration {MigrationStep}", typeof(MatchTeamAddTable).Name);

            if (TableExists(Constants.Tables.MatchTeam) == false)
            {
                Create.Table<MatchTeamTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<MatchTeamAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.MatchTeam);
            }
        }
    }
}