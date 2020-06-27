using System;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class MigratedCompetitionSubscription
    {
        public Guid? CompetitionSubscriptionId { get; set; }
        public Guid? CompetitionId { get; set; }
        public int MigratedCompetitionId { get; set; }
        public Guid? MemberKey { get; set; }
        public string MigratedMemberEmail { get; set; }
        public string DisplayName { get; set; }
        public DateTimeOffset SubscriptionDate { get; set; }
        public string MemberName { get; set; }
    }
}