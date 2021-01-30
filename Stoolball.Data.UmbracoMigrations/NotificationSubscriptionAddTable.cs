using Stoolball.Data.SqlServer;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

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

        public override void Migrate()
        {
            Logger.Debug<NotificationSubscriptionAddTable>("Running migration {MigrationStep}", typeof(NotificationSubscriptionAddTable).Name);

            if (TableExists(Tables.NotificationSubscription) == false)
            {
                Create.Table<NotificationSubscriptionTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<NotificationSubscriptionAddTable>("The database table {DbTable} already exists, skipping", Tables.NotificationSubscription);
            }
        }
    }
}