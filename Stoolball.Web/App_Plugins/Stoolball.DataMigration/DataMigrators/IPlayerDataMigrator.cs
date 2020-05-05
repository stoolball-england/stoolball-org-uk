using Stoolball.Teams;
using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface IPlayerDataMigrator
    {
        Task<PlayerIdentity> MigratePlayer(MigratedPlayerIdentity player);
        Task DeletePlayers();
    }
}