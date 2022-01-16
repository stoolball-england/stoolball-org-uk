namespace Stoolball.Web.Html
{
    public class HtmlSanitizer : Ganss.XSS.HtmlSanitizer, Stoolball.Html.IHtmlSanitizer
    {
        public HtmlSanitizer() : base()
        {
            AllowedTags.Clear();
            AllowedTags.Add("p");
            AllowedTags.Add("h2");
            AllowedTags.Add("strong");
            AllowedTags.Add("em");
            AllowedTags.Add("ul");
            AllowedTags.Add("ol");
            AllowedTags.Add("li");
            AllowedTags.Add("a");
            AllowedTags.Add("br");
            AllowedAttributes.Clear();
            AllowedAttributes.Add("href");
            AllowedCssProperties.Clear();
            AllowedAtRules.Clear();
        }

        /// <inheritdoc/>
        public string Sanitize(string html)
        {
            return base.Sanitize(html);
        }
    }
}
