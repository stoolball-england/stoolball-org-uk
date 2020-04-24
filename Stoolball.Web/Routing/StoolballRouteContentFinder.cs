using System;
using System.Collections.Generic;
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

            var routeTypes = new Dictionary<(string expectedPrefix, string subrouteRegex), StoolballRouteType>
            {
                // Match /prefix/example-entity, but not /prefix, /prefix/, or /prefix/example-entity/invalid, 
                // in upper, lower or mixed case
                { ("clubs", null), StoolballRouteType.Club },
                { ("locations", null), StoolballRouteType.MatchLocation},
                { ("teams", null), StoolballRouteType.Team},
                { ("competitions", null), StoolballRouteType.Competition },

                // Match /competitions/example-entity/2020, /competitions/example-entity/2020-21, 
                // but not /competitions, /competitions/, /competitions/example-entity, /competitions/example-entity/invalid 
                // or /competitions/example-entity/2020/invalid, in upper, lower or mixed case
                { ("competitions", @"[0-9]{4}\/?(-[0-9]{2}\/?)?"), StoolballRouteType.Season },

                // Match /teams/example-team/matches or /teams/example-team/matches/ but not /teams, /teams/
                // /teams/example-team, /teams/example-team/ or /teams/example-team/invalid in upper, lower or mixed case
                { ("clubs", @"matches\/?"), StoolballRouteType.MatchesForClub },
                { ("teams", @"matches\/?"), StoolballRouteType.MatchesForTeam }
            };

            foreach (var routeType in routeTypes)
            {
                if (MatchRouteType(path, routeType.Key.expectedPrefix, routeType.Key.subrouteRegex))
                {
                    return routeType.Value;
                }
            }

            return null;
        }

        private static bool MatchRouteType(string path, string expectedPrefix, string subrouteRegex)
        {
            if (!string.IsNullOrEmpty(subrouteRegex))
            {
                return Regex.IsMatch(path, $@"^\/{expectedPrefix}\/[a-z0-9-]+\/{subrouteRegex}$", RegexOptions.IgnoreCase);
            }
            else
            {
                return Regex.IsMatch(path, $@"^\/{expectedPrefix}\/([a-z0-9-]+\/?)$", RegexOptions.IgnoreCase);
            }
        }
    }
}