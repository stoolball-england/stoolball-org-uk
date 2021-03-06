﻿using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording stoolball competitions
    /// </summary>
    public partial class CompetitionAddTable : MigrationBase
    {
        public CompetitionAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<CompetitionAddTable>("Running migration {MigrationStep}", typeof(CompetitionAddTable).Name);

            if (TableExists(Tables.Competition) == false)
            {
                Create.Table<CompetitionTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<CompetitionAddTable>("The database table {DbTable} already exists, skipping", Tables.Competition);
            }
        }
    }
}