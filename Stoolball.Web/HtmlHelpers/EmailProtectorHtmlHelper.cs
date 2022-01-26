using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Stoolball.Email;
using Umbraco.Cms.Core.Strings;

namespace Stoolball.Web.HtmlHelpers
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
        public static HtmlString ProtectEmailAddresses(this IHtmlHelper helper, IEmailProtector emailProtector, IHtmlEncodedString? html)
        {
            return helper.ProtectEmailAddresses(emailProtector, html?.ToHtmlString(), null);
        }


        /// <summary>
        /// Obfuscate email addresses in HTML and require authentication to view them in full
        /// </summary>
        public static HtmlString ProtectEmailAddresses(this IHtmlHelper helper, IEmailProtector emailProtector, string? html)
        {
            return helper.ProtectEmailAddresses(emailProtector, html, null);
        }

        /// <summary>
        /// Obfuscate email addresses in HTML and require authentication to view them in full
        /// </summary>
        public static HtmlString ProtectEmailAddresses(this IHtmlHelper helper, IEmailProtector emailProtector, string? html, string? currentMemberEmail)
        {
            if (string.IsNullOrWhiteSpace(html)) return new HtmlString(string.Empty);

            if (!string.IsNullOrEmpty(currentMemberEmail))
            {
                html = html.Replace("{{EMAIL}}", currentMemberEmail);
            }

            var userIsAuthenticated = helper?.ViewContext?.HttpContext?.User?.Identity?.IsAuthenticated;
            if (!userIsAuthenticated.HasValue) { userIsAuthenticated = false; }

            return new HtmlString(emailProtector.ProtectEmailAddresses(html, userIsAuthenticated.Value, currentMemberEmail));
        }
    }
}