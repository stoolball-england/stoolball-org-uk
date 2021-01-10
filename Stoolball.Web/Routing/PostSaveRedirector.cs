using System;
using System.Web.Mvc;

namespace Stoolball.Routing
{
    public class PostSaveRedirector : IPostSaveRedirector
    {
        /// <inheritdoc/>
        public RedirectResult WorkOutRedirect(string routeBefore, string routeAfter, string destinationSuffix, string referrer)
        {
            if (!string.IsNullOrEmpty(referrer))
            {
                var routeFromReferrer = new Uri(referrer).AbsolutePath;
                if (!routeFromReferrer.StartsWith(routeBefore, StringComparison.OrdinalIgnoreCase))
                {
                    return new RedirectResult(routeAfter + destinationSuffix);
                }

                if (routeAfter != routeBefore && !string.IsNullOrEmpty(routeBefore))
                {
                    routeFromReferrer = routeAfter + routeFromReferrer.Substring(routeBefore.Length);
                }

                return new RedirectResult(routeFromReferrer, false);
            }

            return new RedirectResult(routeAfter + destinationSuffix, false);
        }
    }
}
