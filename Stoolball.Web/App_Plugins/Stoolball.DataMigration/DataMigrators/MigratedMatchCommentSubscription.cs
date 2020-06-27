using System;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class MigratedMatchCommentSubscription
    {
        public Guid? MatchCommentSubscriptionId { get; set; }
        public Guid? MatchId { get; set; }
        public int MigratedMatchId { get; set; }
        public Guid? MemberKey { get; set; }
        public int MigratedMemberId { get; set; }
        public string MigratedMemberEmail { get; set; }
        public string DisplayName { get; set; }
        public DateTimeOffset SubscriptionDate { get; set; }
        public string MemberName { get; set; }
    }
}