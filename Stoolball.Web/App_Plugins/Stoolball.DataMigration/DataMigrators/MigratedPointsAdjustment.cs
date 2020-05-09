using Stoolball.Competitions;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class MigratedPointsAdjustment : PointsAdjustment
    {
        public int MigratedTeamId { get; set; }
    }
}