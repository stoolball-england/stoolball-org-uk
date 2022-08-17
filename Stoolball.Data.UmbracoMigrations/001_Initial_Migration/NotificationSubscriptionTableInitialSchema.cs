using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.NotificationSubscription)]
    [PrimaryKey(nameof(NotificationSubscriptionId), AutoIncrement = false)]
    [ExplicitColumns]
    public class NotificationSubscriptionTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(NotificationSubscriptionId))]
        public Guid NotificationSubscriptionId { get; set; }

        [Column(nameof(MemberKey))]
        [Index(IndexTypes.Clustered)]
        public Guid MemberKey { get; set; }

        [Column(nameof(NotificationType))]
        public string NotificationType { get; set; }

        [Column(nameof(Query))]
        public string Query { get; set; }

        [Column(nameof(DisplayName))]
        public string DisplayName { get; set; }

        [Column(nameof(DateSubscribed))]
        public DateTime DateSubscribed { get; set; }
    }
}