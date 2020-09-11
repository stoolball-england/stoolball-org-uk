using System.Threading.Tasks;
using Stoolball.Teams;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface IPlayerDataMigrator
    {
        Task<Player> MigratePlayer(MigratedPlayerIdentity player);
        Task DeletePlayers();
    }
}