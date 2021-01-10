using System.Web.Mvc;

namespace Stoolball.Routing
{
    public interface IPostSaveRedirector
    {
        /// <summary>
        /// Redirect back to the referring page (ensuring we don't allow off-site redirects), or the supplied route and suffix if that's not available
        /// </summary>
        RedirectResult WorkOutRedirect(string routeBefore, string routeAfter, string destinationSuffix, string referrer);
    }
}