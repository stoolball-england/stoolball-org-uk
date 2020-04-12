using Stoolball.MatchLocations;
using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface IMatchLocationDataMigrator
    {
        Task<MatchLocation> MigrateMatchLocation(MatchLocation matchLocation);
        Task DeleteMatchLocations();
    }
}