using System.Globalization;
using System.Text.RegularExpressions;

namespace Stoolball.Email
{
    /// <summary>
    /// Links email addresses in HTML, but protecting them from unauthenticated users
    /// </summary>
    public class EmailProtector : IEmailProtector
    {
        // From http://regexlib.com/REDetails.aspx?regexp_id=328
        private const string EMAIL_REGEX = @"((""[^""\f\n\r\t\v\b]+"")|([\w\!\#\$\%\&\'\*\+\-\~\/\^\`\|\{\}]+(\.[\w\!\#\$\%\&\'\*\+\-\~\/\^\`\|\{\}]+)*))@((\[(((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9])))\])|(((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9])))|((([A-Za-z0-9\-])+\.)+[A-Za-z\-]+))";

        /// <summary>
        /// Links email addresses in HTML, but protecting them from unauthenticated users
        /// </summary>
        /// <param name="html">An HTML fragment</param>
        /// <param name="userIsAuthenticated"></param>
        /// <returns>Updated HTML</returns>
        public string ProtectEmailAddresses(string html, bool userIsAuthenticated)
        {
            if (string.IsNullOrEmpty(html)) return html;

            return Regex.Replace(html, EMAIL_REGEX, match =>
            {
                if (userIsAuthenticated)
                {
                    var obfuscatedEmail = Obfuscate(match.Value);
                    return $@"<a href=""&#0109;&#0097;&#0105;&#0108;&#0116;&#0111;&#0058;{obfuscatedEmail}"">{obfuscatedEmail}</a>";
                }
                else
                {
                    return @"(email address available – please <a href=""/account/sign-in"">sign in</a>)";
                }
            });
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
