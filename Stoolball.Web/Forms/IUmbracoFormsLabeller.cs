using Umbraco.Forms.Web.Models;

namespace Stoolball.Web.Forms
{
    public interface IUmbracoFormsLabeller
    {
        /// <summary>
        /// Gets the prefix added to Umbraco Forms page, fieldset and field ids when used as HTML id attributes, which must not start with a digit.
        /// </summary>
        string ElementIdPrefix { get; }

        /// <summary>
        /// Gets the HTML id attribute for an Umbraco Forms page, fieldset or field id.
        /// </summary>
        string ElementId(object id);

        string DescribedBy(FieldViewModel model);
    }
}