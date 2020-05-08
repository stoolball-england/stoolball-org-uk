using Stoolball.Matches;
using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface IPlayerPerformanceDataMigrator
    {
        Task<PlayerInnings> MigratePlayerInnings(MigratedPlayerInnings innings);
        Task DeletePlayerInnings();
        Task<Over> MigrateOver(MigratedOver over);
        Task DeleteOvers();
    }
}