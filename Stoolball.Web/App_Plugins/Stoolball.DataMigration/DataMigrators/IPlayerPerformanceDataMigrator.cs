using Stoolball.Matches;
using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface IPlayerPerformanceDataMigrator
    {
        Task<Batting> MigrateBatting(MigratedBatting batting);
        Task DeleteBatting();
        Task<BowlingOver> MigrateBowling(MigratedBowlingOver bowling);
        Task DeleteBowling();
    }
}