using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

namespace Stoolball.Web.Routing
{
    public class PostSaveRedirector : IPostSaveRedirector
    {
        /// <inheritdoc/>
        public RedirectResult WorkOutRedirect(string routeBefore, string routeAfter, string defaultDestinationSuffix, string referrer, string? permittedReferrerRegex = null)
        {
            if (!string.IsNullOrEmpty(referrer))
            {
                var routeFromReferrer = new Uri(referrer).AbsolutePath;
                if (!routeFromReferrer.StartsWith(routeBefore, StringComparison.OrdinalIgnoreCase))
                {
                    return new RedirectResult(routeAfter + defaultDestinationSuffix);
                }

                if (routeAfter != routeBefore && !string.IsNullOrEmpty(routeBefore))
                {
                    routeFromReferrer = routeAfter + routeFromReferrer.Substring(routeBefore.Length);
                }

                if (string.IsNullOrEmpty(permittedReferrerRegex) || Regex.IsMatch(routeFromReferrer, permittedReferrerRegex, RegexOptions.IgnoreCase))
                {
                    return new RedirectResult(routeFromReferrer, false);
                }
            }

            return new RedirectResult(routeAfter + defaultDestinationSuffix, false);
        }
    }
}
