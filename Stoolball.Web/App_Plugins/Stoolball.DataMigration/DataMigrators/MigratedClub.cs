using Stoolball.Audit;
using System;
using System.Collections.Generic;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class MigratedClub : IAuditable
    {
        public Guid? ClubId { get; set; }
        public int MigratedClubId { get; set; }
        public string ClubName { get; set; }
        public string Twitter { get; set; }
        public string Facebook { get; set; }
        public string Instagram { get; set; }
        public bool ClubMark { get; set; }
        public int MemberGroupId { get; set; }
        public string ClubRoute { get; set; }
        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();
        public Uri EntityUri
        {
            get { return new Uri($"https://www.stoolball.org.uk/id/club/{ClubId}"); }
        }
    }
}