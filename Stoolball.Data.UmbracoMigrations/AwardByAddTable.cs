using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording which entities present awards for performances in stoolball matches, tournaments and seasons
    /// </summary>
    public partial class AwardByAddTable : MigrationBase
    {
        public AwardByAddTable(IMigrationContext context) : base(context)
        {
        }

        public override void Migrate()
        {
            Logger.Debug<AwardByAddTable>("Running migration {MigrationStep}", typeof(AwardByAddTable).Name);

            if (TableExists(Constants.Tables.AwardBy) == false)
            {
                Create.Table<AwardByTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<AwardByAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.AwardBy);
            }
        }
    }
}