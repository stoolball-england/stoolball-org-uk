using Stoolball.Clubs;
using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface IClubDataMigrator
    {
        Task MigrateClub(Club club);
    }
}