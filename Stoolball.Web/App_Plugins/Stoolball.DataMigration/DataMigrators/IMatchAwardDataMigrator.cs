using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface IMatchAwardDataMigrator
    {
        Task<MigratedMatchAward> MigrateMatchAward(MigratedMatchAward award);
        Task DeleteMatchAwards();
    }
}