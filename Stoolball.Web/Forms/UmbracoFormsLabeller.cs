using System;
using Umbraco.Forms.Web.Models;

namespace Stoolball.Web.Forms
{
    public class UmbracoFormsLabeller : IUmbracoFormsLabeller
    {
        public string ElementIdPrefix => "uf-";

        public string ElementId(object id)
        {
            if (id is null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return ElementIdPrefix + id;
        }

        public string DescribedBy(FieldViewModel model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return (string.IsNullOrEmpty(model.ToolTip) ? string.Empty : " " + ElementId(model.Id) + "-tooltip").Trim();
        }
    }
}