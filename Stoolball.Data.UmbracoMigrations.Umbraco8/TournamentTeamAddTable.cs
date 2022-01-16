using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the teams playing in a stoolball tournament
    /// </summary>
    public partial class TournamentTeamAddTable : MigrationBase
    {
        public TournamentTeamAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<TournamentTeamAddTable>("Running migration {MigrationStep}", typeof(TournamentTeamAddTable).Name);

            if (TableExists(Tables.TournamentTeam) == false)
            {
                Create.Table<TournamentTeamTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<TournamentTeamAddTable>("The database table {DbTable} already exists, skipping", Tables.TournamentTeam);
            }
        }
    }
}