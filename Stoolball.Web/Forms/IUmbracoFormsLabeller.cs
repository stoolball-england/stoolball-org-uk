using Umbraco.Forms.Web.Models;

namespace Stoolball.Web.Forms
{
    public interface IUmbracoFormsLabeller
    {
        string DescribedBy(FieldViewModel model);
    }
}