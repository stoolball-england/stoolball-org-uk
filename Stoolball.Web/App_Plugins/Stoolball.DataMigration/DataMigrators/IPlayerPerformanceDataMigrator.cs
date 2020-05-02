using Stoolball.Matches;
using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface IPlayerPerformanceDataMigrator
    {
        Task<Batting> MigrateBatting(Batting batting);
        Task DeleteBatting();
        Task<BowlingOver> MigrateBowling(BowlingOver bowling);
        Task DeleteBowling();
    }
}