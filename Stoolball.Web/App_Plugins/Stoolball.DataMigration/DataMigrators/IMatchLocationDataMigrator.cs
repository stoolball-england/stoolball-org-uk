using Stoolball.MatchLocations;
using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface IMatchLocationDataMigrator
    {
        Task MigrateMatchLocation(MatchLocation matchLocation);
        Task DeleteMatchLocations();
    }
}