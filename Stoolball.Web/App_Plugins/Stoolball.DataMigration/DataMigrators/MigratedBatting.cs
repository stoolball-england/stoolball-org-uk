using Stoolball.Matches;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class MigratedBatting : Batting
    {
        public int MigratedMatchId { get; set; }
        public int MigratedPlayerIdentityId { get; set; }
        public int MigratedTeamId { get; set; }
        public int? MigratedDismissedById { get; set; }
        public int? MigratedBowlerId { get; set; }
    }
}