using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface IMatchCommentDataMigrator
    {
        Task<MigratedMatchComment> MigrateMatchComment(MigratedMatchComment comment);
        Task DeleteMatchComments();
    }
}