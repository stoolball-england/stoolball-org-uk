using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// Adds a table for recording subscriptions to notifications, such as email notifications about new matches
    /// </summary>
    public partial class NotificationSubscriptionAddTable : MigrationBase
    {
        public NotificationSubscriptionAddTable(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", typeof(NotificationSubscriptionAddTable).Name);

            if (TableExists(Tables.NotificationSubscription) == false)
            {
                Create.Table<NotificationSubscriptionTableInitialSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", Tables.NotificationSubscription);
            }
        }
    }
}