﻿using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Data
{
    /// <summary>
    /// Adds a table for recording what bonus or penalty points are awarded or deducted in stoolball seasons
    /// </summary>
    public partial class SeasonPointsAdjustmentAddTable : MigrationBase
    {
        public SeasonPointsAdjustmentAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<SeasonPointsAdjustmentAddTable>("Running migration {MigrationStep}", typeof(SeasonPointsAdjustmentAddTable).Name);

            if (TableExists(Constants.Tables.SeasonPointsAdjustment) == false)
            {
                Create.Table<SeasonPointsAdjustmentTableSchema>().Do();
            }
            else
            {
                Logger.Debug<SeasonPointsAdjustmentAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.SeasonPointsAdjustment);
            }
        }
    }
}