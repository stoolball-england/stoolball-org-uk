using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording what points are awarded for match results in leagues
    /// </summary>
    public partial class PointsRuleAddTable : MigrationBase
    {
        public PointsRuleAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<PointsRuleAddTable>("Running migration {MigrationStep}", typeof(PointsRuleAddTable).Name);

            if (TableExists(Tables.PointsRule) == false)
            {
                Create.Table<PointsRuleTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<PointsRuleAddTable>("The database table {DbTable} already exists, skipping", Tables.PointsRule);
            }
        }
    }
}