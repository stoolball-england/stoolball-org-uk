using Stoolball.Matches;
using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface ITournamentDataMigrator
    {
        Task<Tournament> MigrateTournament(MigratedTournament tournament);
        Task DeleteTournaments();
    }
}