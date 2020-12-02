using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Stoolball.Email
{
    /// <summary>
    /// Links email addresses in HTML, but protecting them from unauthenticated users
    /// </summary>
    public class EmailProtector : IEmailProtector
    {
        // From http://regexlib.com/REDetails.aspx?regexp_id=328
        private const string EMAIL_REGEX = @"((""[^""\f\n\r\t\v\b]+"")|([\w\!\#\$\%\&\'\*\+\-\~\/\^\`\|\{\}]+(\.[\w\!\#\$\%\&\'\*\+\-\~\/\^\`\|\{\}]+)*))@((\[(((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9])))\])|(((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9])))|((([A-Za-z0-9\-])+\.)+[A-Za-z\-]+))";

        private const string REPLACEMENT_HTML_BEFORE_LINK = "(email address available – please ";
        private const string REPLACEMENT_HTML_AFTER_LINK = ")";

        /// <summary>
        /// Links email addresses in HTML, but protecting them from unauthenticated users
        /// </summary>
        /// <param name="html">An HTML fragment</param>
        /// <param name="userIsAuthenticated"></param>
        /// <param name="excludedAddress">An address which may be obfuscated but may not be entirely hidden</param>
        /// <returns>Updated HTML</returns>
        public string ProtectEmailAddresses(string html, bool userIsAuthenticated, string excludedAddress)
        {
            if (string.IsNullOrEmpty(html)) return html;

            var document = new HtmlDocument();
            document.LoadHtml(html);
            var emailLinks = document.DocumentNode.SelectNodes("//a[starts-with(@href,'mailto:')]");
            if (emailLinks != null)
            {
                foreach (HtmlNode node in emailLinks)
                {
                    if (AllowEmailAddress(node.Attributes["href"].Value.Substring(7), userIsAuthenticated, excludedAddress))
                    {
                        node.Attributes["href"].Value = Obfuscate(node.Attributes["href"].Value);
                        node.InnerHtml = Regex.Replace(node.InnerHtml, EMAIL_REGEX, match => Obfuscate(match.Value));
                    }
                    else
                    {
                        ReplaceNodeWithProtectedHtml(node.ParentNode, node);
                    }
                }
            }

            var textNodes = document.DocumentNode.SelectNodes("//text()[normalize-space(.) != '']");
            if (textNodes != null)
            {
                foreach (HtmlTextNode node in textNodes)
                {
                    ProtectTextNode(node, node.ParentNode, userIsAuthenticated, excludedAddress);
                }
            }

            return document.DocumentNode.OuterHtml;
        }

        private static void ReplaceNodeWithProtectedHtml(HtmlNode targetNode, HtmlNode childNode)
        {
            var signInLink = CreateSignInLink(targetNode.OwnerDocument);

            targetNode.ReplaceChild(signInLink, childNode);
            targetNode.InsertBefore(targetNode.OwnerDocument.CreateTextNode(REPLACEMENT_HTML_BEFORE_LINK), signInLink);
            targetNode.InsertAfter(targetNode.OwnerDocument.CreateTextNode(REPLACEMENT_HTML_AFTER_LINK), signInLink);
        }

        private static bool AllowEmailAddress(string emailAddress, bool userIsAuthenticated, string excludedAddress)
        {
            if (userIsAuthenticated) return true;
            if (emailAddress == excludedAddress) return true;
            return false;
        }

        private static void ProtectTextNode(HtmlTextNode textNode, HtmlNode targetNode, bool userIsAuthenticated, string excludedAddress)
        {
            var match = Regex.Match(textNode.OuterHtml, EMAIL_REGEX);
            if (match.Success)
            {
                var beforeMatch = targetNode.OwnerDocument.CreateTextNode(textNode.OuterHtml.Substring(0, match.Index));
                var afterMatch = targetNode.OwnerDocument.CreateTextNode(textNode.OuterHtml.Substring(match.Index + match.Length));

                if (AllowEmailAddress(match.Value, userIsAuthenticated, excludedAddress))
                {
                    var replacement = CreateObfuscatedLink(targetNode.OwnerDocument, match.Value);
                    targetNode.ReplaceChild(replacement, textNode);
                    targetNode.InsertBefore(beforeMatch, replacement);
                    targetNode.InsertAfter(afterMatch, replacement);
                }
                else
                {
                    var signInLink = CreateSignInLink(targetNode.OwnerDocument);

                    targetNode.ReplaceChild(signInLink, textNode);
                    targetNode.InsertBefore(beforeMatch, signInLink);
                    targetNode.InsertBefore(targetNode.OwnerDocument.CreateTextNode(REPLACEMENT_HTML_BEFORE_LINK), signInLink);
                    targetNode.InsertAfter(afterMatch, signInLink);
                    targetNode.InsertAfter(targetNode.OwnerDocument.CreateTextNode(REPLACEMENT_HTML_AFTER_LINK), signInLink);
                }

                ProtectTextNode(afterMatch, afterMatch.ParentNode, userIsAuthenticated, excludedAddress);
            }
        }

        private static HtmlNode CreateSignInLink(HtmlDocument document)
        {
            var link = document.CreateElement("a");
            link.SetAttributeValue("href", "/account/sign-in");
            link.InnerHtml = "sign in";
            return link;
        }

        private static HtmlNode CreateObfuscatedLink(HtmlDocument document, string emailAddress)
        {
            var obfuscatedEmail = Obfuscate(emailAddress);
            var link = document.CreateElement("a");
            link.SetAttributeValue("href", $"&#0109;&#0097;&#0105;&#0108;&#0116;&#0111;&#0058;{obfuscatedEmail}");
            link.InnerHtml = obfuscatedEmail;
            return link;
        }

        /// <summary>
        /// Links email addresses in HTML, but protecting them from unauthenticated users
        /// </summary>
        /// <param name="html">An HTML fragment</param>
        /// <param name="userIsAuthenticated"></param>
        /// <returns>Updated HTML</returns>
        public string ProtectEmailAddresses(string html, bool userIsAuthenticated)
        {
            return ProtectEmailAddresses(html, userIsAuthenticated, null);
        }

        private static string Obfuscate(string text)
        {
            text = text.ToLower(CultureInfo.CurrentCulture)
                .Replace(".", "&#0046;")
                .Replace(":", "&#0058;")
                .Replace("@", "&#0064;")
                .Replace("a", "&#0097;")
                .Replace("b", "&#0098;")
                .Replace("c", "&#0099;")
                .Replace("d", "&#0100;")
                .Replace("e", "&#0101;")
                .Replace("f", "&#0102;")
                .Replace("g", "&#0103;")
                .Replace("h", "&#0104;")
                .Replace("i", "&#0105;")
                .Replace("j", "&#0106;")
                .Replace("k", "&#0107;")
                .Replace("l", "&#0108;")
                .Replace("m", "&#0109;")
                .Replace("n", "&#0110;")
                .Replace("o", "&#0111;")
                .Replace("p", "&#0112;")
                .Replace("q", "&#0113;")
                .Replace("r", "&#0114;")
                .Replace("s", "&#0115;")
                .Replace("t", "&#0116;")
                .Replace("u", "&#0117;")
                .Replace("v", "&#0118;")
                .Replace("w", "&#0119;")
                .Replace("x", "&#0120;")
                .Replace("y", "&#0121;")
                .Replace("z", "&#0122;");
            return text;
        }
    }
}
