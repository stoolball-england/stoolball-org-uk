using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface ICompetitionSubscriptionDataMigrator
    {
        Task<MigratedCompetitionSubscription> MigrateCompetitionSubscription(MigratedCompetitionSubscription subscription);
        Task DeleteCompetitionSubscriptions();
    }
}