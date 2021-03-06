﻿using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the teams playing in a stoolball match
    /// </summary>
    public partial class MatchTeamAddTable : MigrationBase
    {
        public MatchTeamAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<MatchTeamAddTable>("Running migration {MigrationStep}", typeof(MatchTeamAddTable).Name);

            if (TableExists(Tables.MatchTeam) == false)
            {
                Create.Table<MatchTeamTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<MatchTeamAddTable>("The database table {DbTable} already exists, skipping", Tables.MatchTeam);
            }
        }
    }
}