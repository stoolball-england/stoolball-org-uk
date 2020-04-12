using Stoolball.Schools;
using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface ISchoolDataMigrator
    {
        Task<School> MigrateSchool(School school);
        Task DeleteSchools();
    }
}