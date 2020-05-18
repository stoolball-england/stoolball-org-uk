using Stoolball.Teams;
using System;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class MigratedTeam : Team
    {
        public int MigratedTeamId { get; set; }
        public int? MigratedClubId { get; set; }
        public int? MigratedSchoolId { get; set; }
        public int? MigratedMatchLocationId { get; set; }
        public Guid? ClubId { get; set; }
        public Guid? SchoolId { get; set; }
        public Guid? MatchLocationId { get; set; }
    }
}