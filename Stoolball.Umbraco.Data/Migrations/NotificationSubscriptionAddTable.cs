using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Data.Migrations
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

            if (TableExists(Constants.Tables.NotificationSubscription) == false)
            {
                Create.Table<NotificationSubscriptionTableInitialSchema>().Do();
            }
            else
            {
                Logger.Debug<NotificationSubscriptionAddTable>("The database table {DbTable} already exists, skipping", Constants.Tables.NotificationSubscription);
            }
        }
    }
}