using System;
using Umbraco.Forms.Mvc.Models;

namespace Stoolball.Web.Forms
{
    public class UmbracoFormsLabeller : IUmbracoFormsLabeller
    {
        public string DescribedBy(FieldViewModel model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return (string.IsNullOrEmpty(model.ToolTip) ? string.Empty : " " + model.Id + "-tooltip").Trim();
        }
    }
}