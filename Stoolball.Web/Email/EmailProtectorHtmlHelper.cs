using Stoolball.Email;
using System.Diagnostics.CodeAnalysis;
using System.Web;
using System.Web.Mvc;
using Current = Umbraco.Web.Composing.Current;

namespace Stoolball.Web.Email
{
    /// <summary>
    /// A wrapper for any <see cref="IEmailProtector"/> which makes its use far less complex in a view.
    /// </summary>
    /// <remarks>This is not tested because it just adds access to context objects to the base implementation of 
    /// <see cref="IEmailProtector"/>. The base implementation should be tested.</remarks>
    [ExcludeFromCodeCoverage]
    public static class EmailProtectorHtmlHelper
    {
        /// <summary>
        /// Obfuscate email addresses in HTML and require authentication to view them in full
        /// </summary>
        public static IHtmlString ProtectEmailAddresses(this HtmlHelper helper, IHtmlString html)
        {
            return ProtectEmailAddresses(helper, html?.ToHtmlString());
        }

        /// <summary>
        /// Obfuscate email addresses in HTML and require authentication to view them in full
        /// </summary>
        public static IHtmlString ProtectEmailAddresses(this HtmlHelper helper, string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return new HtmlString(string.Empty);

            var emailProtector = Current.Factory.GetInstance(typeof(IEmailProtector)) as IEmailProtector;
            if (emailProtector == null) return new HtmlString(html);

            var userIsAuthenticated = helper?.ViewContext?.HttpContext?.User?.Identity?.IsAuthenticated;
            if (!userIsAuthenticated.HasValue) { userIsAuthenticated = false; }

            return new HtmlString(emailProtector.ProtectEmailAddresses(html, userIsAuthenticated.Value));
        }
    }
}