using System;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class MigratedMatchComment
    {
        public Guid? MatchCommentId { get; set; }
        public Guid? MatchId { get; set; }
        public int MigratedMatchId { get; set; }
        public Guid? MemberKey { get; set; }
        public string MemberName { get; set; }
        public int MigratedMemberId { get; set; }
        public string MigratedMemberEmail { get; set; }
        public string Comment { get; set; }
        public DateTimeOffset CommentDate { get; set; }
    }
}