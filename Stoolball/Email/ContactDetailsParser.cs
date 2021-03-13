using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Stoolball.Email
{
    public class ContactDetailsParser : IContactDetailsParser
    {
        public string ParseFirstEmailAddress(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }

            var document = new HtmlDocument();
            document.LoadHtml(html);
            var nodes = document.DocumentNode.SelectNodes("//a[starts-with(@href,'mailto:')]|//text()[normalize-space(.) != '']");
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    var textNode = node as HtmlTextNode;
                    if (textNode != null)
                    {
                        var email = Regex.Match(textNode.OuterHtml, Constants.RegularExpressions.Email, RegexOptions.IgnoreCase);
                        if (email.Success)
                        {
                            return email.Value;
                        }
                    }
                    else
                    {
                        return node.GetAttributeValue("href", null).Substring(7);
                    }
                }
            }
            return null;
        }

        public string ParseFirstPhoneNumber(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }

            var phone = Regex.Match(html, @"\b([0-9)( ]{11,14})\b");
            return phone.Success ? phone.Value.Replace("(", string.Empty).Replace(")", string.Empty).Trim() : null;
        }
    }
}
