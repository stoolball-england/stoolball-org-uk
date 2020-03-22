using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Migrations
{
    /// <summary>
    /// Adds a table for recording teams playing in stoolball seasons
    /// </summary>
    public partial class SeasonTeamAddTable : MigrationBase
    {
        public SeasonTeamAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<SeasonTeamAddTable>("Running migration {MigrationStep}", typeof(SeasonTeamAddTable).Name);

            if (TableExists(Constants.Tables.SeasonTeam) == false)
            {
                Create.Table<SeasonTeamTableSchema>().Do();
            }
            else
            {
                Logger.Debug<SeasonTeamAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.SeasonTeam);
            }
        }
    }
}