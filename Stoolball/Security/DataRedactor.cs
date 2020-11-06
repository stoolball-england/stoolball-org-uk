using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Stoolball.Security
{
    public class DataRedactor : IDataRedactor
    {
        // From http://regexlib.com/REDetails.aspx?regexp_id=328
        private const string EMAIL_REGEX = @"((""[^""\f\n\r\t\v\b]+"")|([\w\!\#\$\%\&\'\*\+\-\~\/\^\`\|\{\}]+(\.[\w\!\#\$\%\&\'\*\+\-\~\/\^\`\|\{\}]+)*))@((\[(((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9])))\])|(((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9])))|((([A-Za-z0-9\-])+\.)+[A-Za-z\-]+))";

        private const string UK_PHONE_REGEX = @"(\+44|0044|0|\(0)[0-9 ()-]{9,}[0-9]";

        public string RedactAll(string unredacted)
        {
            if (string.IsNullOrWhiteSpace(unredacted)) { return unredacted; }
            var redacted = new HtmlDocument();
            redacted.LoadHtml(unredacted);
            foreach (HtmlNode node in redacted.DocumentNode.SelectNodes("//text()[normalize-space(.) != '']"))
            {
                node.ParentNode.ReplaceChild(HtmlNode.CreateNode(Regex.Replace(node.InnerText, "[A-Z0-9]", "*", RegexOptions.IgnoreCase)), node);
            }
            return redacted.DocumentNode.OuterHtml;
        }

        public string RedactPersonalData(string unredacted)
        {
            if (string.IsNullOrWhiteSpace(unredacted)) { return unredacted; }
            var redacted = new HtmlDocument();
            redacted.LoadHtml(unredacted);
            foreach (HtmlNode node in redacted.DocumentNode.SelectNodes("//text()[normalize-space(.) != '']"))
            {
                var redactedText = Regex.Replace(node.InnerText, EMAIL_REGEX, "*****@*****.***", RegexOptions.IgnoreCase);
                redactedText = Regex.Replace(redactedText, UK_PHONE_REGEX, "***** ******", RegexOptions.IgnoreCase);
                node.ParentNode.ReplaceChild(HtmlNode.CreateNode(redactedText), node);
            }
            return redacted.DocumentNode.OuterHtml;
        }
    }
}
