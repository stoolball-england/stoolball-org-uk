using Stoolball.Matches;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class MigratedMatchInnings : MatchInnings
    {
        public int? MigratedTeamId { get; set; }
    }
}