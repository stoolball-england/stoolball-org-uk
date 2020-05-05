using Stoolball.Matches;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class MigratedTeamInMatch : TeamInMatch
    {
        public int MigratedTeamId { get; set; }
    }
}