﻿using HtmlAgilityPack;

namespace Stoolball.Html
{
    public class HtmlFormatter : IHtmlFormatter
    {
        public string FormatHtml(string html)
        {
            var parser = new HtmlDocument();
            parser.LoadHtml(html);
            var tables = parser.DocumentNode.SelectNodes("//table");
            if (tables != null)
            {
                foreach (var table in tables)
                {
                    if (!table.HasClass("table")) { table.AddClass("table"); }
                }
            }

            var paragraphs = parser.DocumentNode.SelectNodes("//p");
            if (paragraphs != null)
            {
                foreach (var paragraph in paragraphs)
                {
                    if (string.IsNullOrWhiteSpace(paragraph.InnerHtml)) { paragraph.Remove(); }
                }
            }
            return parser.DocumentNode.OuterHtml;
        }
    }
}
