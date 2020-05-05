using Stoolball.Matches;
using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface IMatchDataMigrator
    {
        Task<Match> MigrateMatch(MigratedMatch match);
        Task<Tournament> MigrateTournament(MigratedTournament tournament);
        Task DeleteMatches();
    }
}