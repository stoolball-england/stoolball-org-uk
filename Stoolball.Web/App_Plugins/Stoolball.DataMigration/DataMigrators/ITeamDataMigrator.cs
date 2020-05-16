using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface ITeamDataMigrator
    {
        Task<MigratedTeam> MigrateTeam(MigratedTeam team);
        Task DeleteTeams();
    }
}