using Stoolball.Teams;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class MigratedTeam : Team
    {
        public int MigratedTeamId { get; set; }
        public int? MigratedClubId { get; set; }
        public int? MigratedSchoolId { get; set; }
        public int? MigratedMatchLocationId { get; set; }
    }
}