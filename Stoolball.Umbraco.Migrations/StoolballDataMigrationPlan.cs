using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Data
{
    // Create a migration plan for the stoolball schema
    // Each latest migration state/step is tracked for this project/feature
    public class StoolballMigrationPlan : MigrationPlan
    {
        public StoolballMigrationPlan() : base("StoolballData")
        {
            // This is the steps we need to take
            // Each step in the migration adds a unique value
            From(string.Empty)
                .To<AuditAddTable>(typeof(AuditAddTable).ToString());
        }
    }
}
