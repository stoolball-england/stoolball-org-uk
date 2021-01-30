using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording the types of awards for performances in stoolball matches and tournaments
    /// </summary>
    public partial class AwardAddTable : MigrationBase
    {
        public AwardAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<AwardAddTable>("Running migration {MigrationStep}", typeof(AwardAddTable).Name);

            if (TableExists(Tables.Award) == false)
            {
                Create.Table<AwardTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<AwardAddTable>("The database table {DbTable} already exists, skipping", Tables.Award);
            }
        }
    }
}