using Stoolball.Competitions;
using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface ICompetitionDataMigrator
    {
        Task<Competition> MigrateCompetition(Competition competition);
        Task DeleteCompetitions();
        Task<Season> MigrateSeason(Season season);
        Task DeleteSeasons();
    }
}