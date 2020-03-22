using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Migrations
{
    /// <summary>
    /// Adds a table for recording what points are awarded for match results in stoolball seasons
    /// </summary>
    public partial class SeasonPointsRuleAddTable : MigrationBase
    {
        public SeasonPointsRuleAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<SeasonPointsRuleAddTable>("Running migration {MigrationStep}", typeof(SeasonPointsRuleAddTable).Name);

            if (TableExists(Constants.Tables.SeasonPointsRule) == false)
            {
                Create.Table<SeasonPointsRuleTableSchema>().Do();
            }
            else
            {
                Logger.Debug<SeasonPointsRuleAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.SeasonPointsRule);
            }
        }
    }
}