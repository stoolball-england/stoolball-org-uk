using Stoolball.Matches;
using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface IMatchDataMigrator
    {
        Task<Match> MigrateMatch(Match match);
        Task<Tournament> MigrateTournament(Tournament tournament);
        Task DeleteMatches();
    }
}