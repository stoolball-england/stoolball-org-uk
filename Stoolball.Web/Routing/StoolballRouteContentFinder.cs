using System;
using System.Text.RegularExpressions;
using Umbraco.Core;
using Umbraco.Web;
using Umbraco.Web.Routing;

namespace Stoolball.Web.Routing
{
    /// <summary>
    /// Looks for routes that correspond to stoolball entities, and directs them to an 
    /// instance of the 'Stoolball router' document type where it's handled by <see cref="StoolballRouterController"/>.
    /// </summary>
    public class StoolballRouteContentFinder : IContentFinder
    {
        public bool TryFindContent(PublishedRequest request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var matchedRouteType = MatchStoolballRouteType(request.Uri);
            if (matchedRouteType.HasValue)
            {
                // Direct the response to the 'Stoolball router' document type to be handled by StoolballRouterController
                var router = request.UmbracoContext.Content.GetSingleByXPath("//stoolballRouter");

                if (router != null)
                {
                    request.PublishedContent = router;
                    request.TrySetTemplate(matchedRouteType.Value.ToString());
                    return request.HasTemplate && router.IsAllowedTemplate(request.TemplateAlias);
                }
            }

            return false;
        }

        /// <summary>
        /// Matches a request URL to a route reserved for stoolball entities
        /// </summary>
        /// <param name="requestUrl">The request URL to test</param>
        /// <returns>The stoolball route type matching the URL, or <c>null</c> for no match</returns>
        internal static StoolballRouteType? MatchStoolballRouteType(Uri requestUrl)
        {
            var path = requestUrl.GetAbsolutePathDecoded();
            StoolballRouteType? matchedType = null;

            // Match /clubs, /clubs/ or /clubs/example-club, but not /clubs/example-club/invalid, in upper, lower or mixed case
            if (Regex.IsMatch(path, @"^\/clubs\/?([a-z0-9-]+\/?)?$", RegexOptions.IgnoreCase))
            {
                matchedType = StoolballRouteType.Club;
            }

            return matchedType;
        }
    }
}