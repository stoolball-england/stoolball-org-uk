using Stoolball.Teams;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class MigratedPlayerIdentity : PlayerIdentity
    {
        public int MigratedPlayerIdentityId { get; set; }
        public int MigratedTeamId { get; set; }
    }
}