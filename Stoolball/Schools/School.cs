using Stoolball.Audit;
using System;
using System.Collections.Generic;

namespace Stoolball.Schools
{
    public class School : IAuditable
    {
        public Guid? SchoolId { get; set; }
        public string SchoolName { get; set; }
        public string Website { get; set; }
        public string Twitter { get; set; }
        public string Facebook { get; set; }
        public string Instagram { get; set; }
        public string YouTube { get; set; }
        public int MemberGroupId { get; set; }
        public string MemberGroupName { get; set; }
        public string SchoolRoute { get; set; }
        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();

        public Uri EntityUri
        {
            get { return new Uri($"https://www.stoolball.org.uk/id/school/{SchoolId}"); }
        }
    }
}