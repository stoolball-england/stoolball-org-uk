using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a function for returning the best team name at a given date
    /// </summary>
    public partial class Add_fnTeamName : MigrationBase
    {
        public Add_fnTeamName(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(Add_fnTeamName).Name);

            Execute.SqlFromFile("037_fn_TeamName.fn_TeamName.sql").Do();
        }
    }
}