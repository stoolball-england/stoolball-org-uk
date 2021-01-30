using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

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

        public override void Migrate()
        {
            Logger.Debug<SeasonTeamAddTable>("Running migration {MigrationStep}", typeof(SeasonTeamAddTable).Name);

            if (TableExists(Tables.SeasonTeam) == false)
            {
                Create.Table<SeasonTeamTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<SeasonTeamAddTable>("The database table {DbTable} already exists, skipping", Tables.SeasonTeam);
            }
        }
    }
}