using Umbraco.Forms.Mvc.Models;

namespace Stoolball.Web.Forms
{
    public interface IUmbracoFormsLabeller
    {
        string DescribedBy(FieldViewModel model);
    }
}