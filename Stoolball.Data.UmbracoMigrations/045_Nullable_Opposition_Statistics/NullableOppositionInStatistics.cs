using System;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Makes the OppositionTeamId, OppositionTeamName, and OppositionTeamRoute columns in StoolballPlayerInMatchStatistics nullable,
    /// because when an opposition team is deleted the statistics for the match should still be retained, but the opposition team information will be lost.
    /// </summary>
    public partial class NullableOppositionInStatistics : MigrationBase
    {
        public NullableOppositionInStatistics(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(NullableOppositionInStatistics).Name);

            try
            {
                Execute.SqlFromFile("045_Nullable_Opposition_Statistics.Make_Opposition_Columns_Nullable.sql").Do();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Migration failed");
                if (ex.InnerException != null) { Logger.LogError(ex.InnerException, ex.InnerException.Message); }
            }
        }
    }
}