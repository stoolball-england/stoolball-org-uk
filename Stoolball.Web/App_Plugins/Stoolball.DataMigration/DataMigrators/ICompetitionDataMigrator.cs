using Stoolball.Competitions;
using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface ICompetitionDataMigrator
    {
        Task MigrateCompetition(Competition competition);
        Task DeleteCompetitions();
        Task MigrateSeason(Season season);
        Task DeleteSeasons();
    }
}