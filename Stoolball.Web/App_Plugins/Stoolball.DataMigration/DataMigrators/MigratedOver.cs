using Stoolball.Matches;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class MigratedOver : Over
    {
        public int MigratedMatchId { get; set; }
        public Match Match { get; set; }
        public int MigratedPlayerIdentityId { get; set; }
        public int MigratedTeamId { get; set; }
        public int MigratedMatchTeamId { get; set; }
    }
}