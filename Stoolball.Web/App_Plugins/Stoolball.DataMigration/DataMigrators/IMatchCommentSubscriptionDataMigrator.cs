using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface IMatchCommentSubscriptionDataMigrator
    {
        Task<MigratedMatchCommentSubscription> MigrateMatchCommentSubscription(MigratedMatchCommentSubscription subscription);
        Task DeleteMatchCommentSubscriptions();
    }
}