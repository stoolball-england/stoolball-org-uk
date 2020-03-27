using System;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.Apis
{
    public class ImportedMember
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int TotalLogins { get; set; }
        public DateTime LastLogin { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
    }
}