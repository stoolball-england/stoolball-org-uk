using System;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Stoolball.Web.HtmlHelpers
{
    /// <summary>
    /// Additional overloads for @Html.LabelFor that support adding an (optional) suffix.
    /// </summary>
    public static class LabelExtensions
    {
        public static IHtmlContent LabelFor<TModel, TValue>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TValue>> expression, string? labelText, RequiredFieldStatus required)
        {
            return htmlHelper.LabelFor(expression, labelText, required, null);
        }

        public static IHtmlContent LabelFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, RequiredFieldStatus required, object? htmlAttributes)
        {
            return htmlHelper.LabelFor(expression, null, required, htmlAttributes);
        }

        public static IHtmlContent LabelFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, string? labelText, RequiredFieldStatus required, object? htmlAttributes)
        {
            var baseResult = (TagBuilder)htmlHelper.LabelFor(expression, labelText: labelText, htmlAttributes: htmlAttributes);

            if (required != RequiredFieldStatus.Required)
            {
                using (var writer = new StringWriter())
                {
                    baseResult.RenderStartTag().WriteTo(writer, HtmlEncoder.Default);
                    if (baseResult.HasInnerHtml) { baseResult.InnerHtml.WriteTo(writer, HtmlEncoder.Default); }
                    writer.Write($" <small>({required.ToString().ToLower(CultureInfo.CurrentCulture)})</small>");
                    baseResult.RenderEndTag().WriteTo(writer, HtmlEncoder.Default);
                    return new HtmlString(writer.ToString());
                }
            }
            return baseResult;
        }
    }
}