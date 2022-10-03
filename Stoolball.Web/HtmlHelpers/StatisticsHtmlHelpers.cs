using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using Umbraco.Extensions;

namespace Stoolball.Web.HtmlHelpers
{
    public static class StatisticsHtmlHelpers
    {
        public static string LinkToStatisticsTable(this IHtmlHelper html, string statisticsUrlSegment, string? queryString)
        {
            return (html.ViewContext.HttpContext.Request.Path.Value ?? string.Empty).TrimEnd("/").TrimEnd("/batting").TrimEnd("/bowling").TrimEnd("/fielding") + "/" + statisticsUrlSegment + queryString;
        }
    }
}