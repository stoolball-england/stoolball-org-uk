using System;

namespace Stoolball.Comments
{
    public class HtmlComment
    {
        public Guid? CommentId { get; set; }
        public Guid MemberKey { get; set; }
        public string? MemberName { get; set; }

        public string? MemberEmail { get; set; }
        public DateTimeOffset CommentDate { get; set; }
        public string? Comment { get; set; }
    }
}
