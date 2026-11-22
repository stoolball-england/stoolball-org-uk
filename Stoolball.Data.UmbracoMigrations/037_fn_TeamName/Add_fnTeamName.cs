using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a function for returning the best team name at a given date
    /// </summary>
    public partial class Add_fnTeamName : AsyncMigrationBase
    {
        public Add_fnTeamName(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(Add_fnTeamName).Name);

            Execute.SqlFromFile("037_fn_TeamName.fn_TeamName.sql").Do();

            return Task.CompletedTask;
        }
    }
}