using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording derived data about players' performances in stoolball matches
    /// </summary>
    public partial class StatisticsPlayerMatchAddTable : MigrationBase
    {
        public StatisticsPlayerMatchAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<StatisticsPlayerMatchAddTable>("Running migration {MigrationStep}", typeof(StatisticsPlayerMatchAddTable).Name);

            if (TableExists(Constants.Tables.StatisticsPlayerMatch) == false)
            {
                Create.Table<StatisticsPlayerMatchTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<StatisticsPlayerMatchAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.StatisticsPlayerMatch);
            }
        }
    }
}