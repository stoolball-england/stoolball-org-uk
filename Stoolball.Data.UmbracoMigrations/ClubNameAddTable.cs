﻿using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the names of stoolball clubs which may change over time
    /// </summary>
    public partial class ClubNameAddTable : MigrationBase
    {
        public ClubNameAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<ClubNameAddTable>("Running migration {MigrationStep}", typeof(ClubNameAddTable).Name);

            if (TableExists(Constants.Tables.ClubName) == false)
            {
                Create.Table<ClubNameTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<ClubNameAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.ClubName);
            }
        }
    }
}