using Microsoft.AspNetCore.Mvc;

namespace Stoolball.Web.Routing
{
    public interface IPostSaveRedirector
    {
        /// <summary>
        /// Redirect back to the referring page (ensuring we don't allow off-site redirects), or the supplied route and suffix if that's not available
        /// </summary>
        RedirectResult WorkOutRedirect(string routeBefore, string routeAfter, string defaultDestinationSuffix, string referrer, string? permittedReferrerRegex = null);
    }
}