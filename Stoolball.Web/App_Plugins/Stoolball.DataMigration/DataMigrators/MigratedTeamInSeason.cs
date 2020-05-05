using Stoolball.Competitions;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class MigratedTeamInSeason : TeamInSeason
    {
        public int MigratedTeamId { get; set; }
    }
}