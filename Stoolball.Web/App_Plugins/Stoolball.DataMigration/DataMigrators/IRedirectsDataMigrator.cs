using System.Threading.Tasks;
using Umbraco.Web;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface IRedirectsDataMigrator
    {
        Task EnsureRedirects(IPublishedContentQuery publishedContentQuery);
    }
}