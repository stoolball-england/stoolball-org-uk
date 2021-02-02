using System.Threading.Tasks;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public interface IUmbracoFormsDataMigrator
    {
        Task RecreateForms();
    }
}