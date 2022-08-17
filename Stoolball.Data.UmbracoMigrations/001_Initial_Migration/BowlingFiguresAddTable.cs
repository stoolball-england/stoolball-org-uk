using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording bowling statistics for a player identity in stoolball matches
    /// </summary>
    public partial class BowlingFiguresAddTable : MigrationBase
    {
        public BowlingFiguresAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(BowlingFiguresAddTable).Name);

            if (TableExists(Tables.BowlingFigures) == false)
            {
                Create.Table<BowlingFiguresTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.BowlingFigures);
            }
        }
    }
}