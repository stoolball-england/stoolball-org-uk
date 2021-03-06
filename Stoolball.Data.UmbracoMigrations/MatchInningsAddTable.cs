﻿using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording team innings in stoolball matches
    /// </summary>
    public partial class MatchInningsAddTable : MigrationBase
    {
        public MatchInningsAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<MatchInningsAddTable>("Running migration {MigrationStep}", typeof(MatchInningsAddTable).Name);

            if (TableExists(Tables.MatchInnings) == false)
            {
                Create.Table<MatchInningsTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<MatchInningsAddTable>("The database table {DbTable} already exists, skipping", Tables.MatchInnings);
            }
        }
    }
}