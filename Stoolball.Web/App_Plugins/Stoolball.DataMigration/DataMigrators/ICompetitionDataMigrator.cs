using Stoolball.Competitions;
using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface ICompetitionDataMigrator
    {
        Task<Competition> MigrateCompetition(MigratedCompetition competition);
        Task DeleteCompetitions();
        Task<Season> MigrateSeason(MigratedSeason season);
        Task DeleteSeasons();
    }
}