using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface IClubDataMigrator
    {
        Task<MigratedClub> MigrateClub(MigratedClub club);
        Task DeleteClubs();
    }
}