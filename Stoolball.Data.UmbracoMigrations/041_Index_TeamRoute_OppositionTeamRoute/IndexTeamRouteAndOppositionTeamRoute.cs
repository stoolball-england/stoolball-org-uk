using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds indexes to the StoolballPlayerInMatchStatistics.TeamRoute and OppositionTeamRoute columns so that the queries from team filters do not need a table scan.
    /// </summary>
    public partial class IndexTeamRouteAndOppositionTeamRoute : MigrationBase
    {
        public IndexTeamRouteAndOppositionTeamRoute(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(IndexTeamRouteAndOppositionTeamRoute).Name);

            Execute.SqlFromFile("041_Index_TeamRoute_OppositionTeamRoute.IX_StoolballPlayerInMatchStatistics_TeamRoute.sql").Do();
            Execute.SqlFromFile("041_Index_TeamRoute_OppositionTeamRoute.IX_StoolballPlayerInMatchStatistics_OppositionTeamRoute.sql").Do();
        }
    }
}