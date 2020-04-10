using Stoolball.Teams;
using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface ITeamDataMigrator
    {
        Task MigrateTeam(Team team);
        Task DeleteTeams();
    }
}