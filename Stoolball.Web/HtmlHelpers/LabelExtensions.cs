using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Routing;

namespace Stoolball.Web.HtmlHelpers
{
    /// <summary>
    /// Additional overloads for @Html.LabelFor that support adding an (optional) suffix.
    /// </summary>
    /// <remarks>Based on code from https://stackoverflow.com/questions/5196290/how-can-i-override-the-html-labelfor-template</remarks>
    public static class LabelExtensions
    {
        public static MvcHtmlString LabelFor<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, RequiredFieldStatus required, object htmlAttributes)
        {
            return LabelFor(html, expression, required, new RouteValueDictionary(htmlAttributes));
        }
        public static MvcHtmlString LabelFor<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, RequiredFieldStatus required, IDictionary<string, object> htmlAttributes)
        {
            ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, html.ViewData);
            string htmlFieldName = ExpressionHelper.GetExpressionText(expression);
            string labelText = metadata.DisplayName ?? metadata.PropertyName ?? htmlFieldName.Split('.').Last();
            if (String.IsNullOrEmpty(labelText))
            {
                return MvcHtmlString.Empty;
            }

            TagBuilder tag = new TagBuilder("label");
            tag.MergeAttributes(htmlAttributes);
            tag.Attributes.Add("for", html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldId(htmlFieldName));
            tag.InnerHtml = labelText;

            if (required != RequiredFieldStatus.Required)
            {
                TagBuilder small = new TagBuilder("small");
                small.SetInnerText($"({required.ToString().ToLower(CultureInfo.CurrentCulture)})");
                tag.InnerHtml = labelText + " " + small.ToString(TagRenderMode.Normal);
            }

            return MvcHtmlString.Create(tag.ToString(TagRenderMode.Normal));
        }
    }
}