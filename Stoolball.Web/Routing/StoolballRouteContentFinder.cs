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
            const string ANY_VALID_ROUTE = "[a-z0-9-]+";
            const string SLASH = @"\/";
            const string OPTIONAL_SLASH = @"\/?";

            var routeTypes = new Dictionary<string, StoolballRouteType>
            {
                // Match /prefix or /prefix/ but not /prefix/invalid, in upper, lower or mixed case
                { $@"clubs{OPTIONAL_SLASH}", StoolballRouteType.Clubs },
                { $@"competitions{OPTIONAL_SLASH}", StoolballRouteType.Competitions },
                { $@"teams{OPTIONAL_SLASH}", StoolballRouteType.Teams },
                { $@"locations{OPTIONAL_SLASH}", StoolballRouteType.MatchLocations },

                // Match /prefix/example-entity or /prefix/action, but not /prefix, /prefix/, or /prefix/example-entity/invalid, 
                // in upper, lower or mixed case
                { $@"clubs{SLASH}add{OPTIONAL_SLASH}", StoolballRouteType.CreateClub },
                { $@"clubs{SLASH}{ANY_VALID_ROUTE}{OPTIONAL_SLASH}", StoolballRouteType.Club },
                { $@"teams{SLASH}add{OPTIONAL_SLASH}", StoolballRouteType.CreateTeam },
                { $@"teams{SLASH}{ANY_VALID_ROUTE}{OPTIONAL_SLASH}", StoolballRouteType.Team},
                { $@"locations{SLASH}add{OPTIONAL_SLASH}", StoolballRouteType.CreateMatchLocation},
                { $@"locations{SLASH}{ANY_VALID_ROUTE}{OPTIONAL_SLASH}", StoolballRouteType.MatchLocation},
                { $@"competitions{SLASH}add{OPTIONAL_SLASH}", StoolballRouteType.CreateCompetition },
                { $@"competitions{SLASH}{ANY_VALID_ROUTE}{OPTIONAL_SLASH}", StoolballRouteType.Competition },
                { $@"matches{SLASH}{ANY_VALID_ROUTE}{OPTIONAL_SLASH}", StoolballRouteType.Match },
                { $@"tournaments{SLASH}{ANY_VALID_ROUTE}{OPTIONAL_SLASH}", StoolballRouteType.Tournament },

                // Match /competitions/example-entity/2020, /competitions/example-entity/2020-21, 
                // but not /competitions, /competitions/, /competitions/example-entity, /competitions/example-entity/invalid 
                // or /competitions/example-entity/2020/invalid, in upper, lower or mixed case
                { $@"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}[0-9]{{4}}{OPTIONAL_SLASH}(-[0-9]{{2}}{OPTIONAL_SLASH})?", StoolballRouteType.Season },
                
                // Match /competitions/example-entity/2020/matches, /competitions/example-entity/2020-21/matches/, 
                // but not /competitions, /competitions/, /competitions/example-entity/2020, /competitions/example-entity/invalid 
                // or /competitions/example-entity/2020/invalid, in upper, lower or mixed case
                { $@"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}[0-9]{{4}}(-[0-9]{{2}})?{SLASH}matches{OPTIONAL_SLASH}", StoolballRouteType.MatchesForSeason },
                { $@"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}[0-9]{{4}}(-[0-9]{{2}})?{SLASH}edit{OPTIONAL_SLASH}", StoolballRouteType.SeasonActions },
                { $@"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}[0-9]{{4}}(-[0-9]{{2}})?{SLASH}edit{SLASH}season{OPTIONAL_SLASH}", StoolballRouteType.EditSeason },
                { $@"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}[0-9]{{4}}(-[0-9]{{2}})?{SLASH}delete{OPTIONAL_SLASH}", StoolballRouteType.DeleteSeason },

                // Match /teams/example-team/valid or /teams/example-team/valid/ but not /teams, /teams/
                // /teams/example-team, /teams/example-team/ or /teams/example-team/invalid in upper, lower or mixed case
                { $@"clubs{SLASH}{ANY_VALID_ROUTE}{SLASH}matches{OPTIONAL_SLASH}", StoolballRouteType.MatchesForClub },
                { $@"clubs{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{OPTIONAL_SLASH}", StoolballRouteType.ClubActions },
                { $@"clubs{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{SLASH}club{OPTIONAL_SLASH}", StoolballRouteType.EditClub },
                { $@"clubs{SLASH}{ANY_VALID_ROUTE}{SLASH}delete{OPTIONAL_SLASH}", StoolballRouteType.DeleteClub },
                { $@"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}matches{OPTIONAL_SLASH}", StoolballRouteType.MatchesForTeam },
                { $@"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{OPTIONAL_SLASH}", StoolballRouteType.TeamActions },
                { $@"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{SLASH}team{OPTIONAL_SLASH}", StoolballRouteType.EditTeam },
                { $@"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}delete{OPTIONAL_SLASH}", StoolballRouteType.DeleteTeam },
                { $@"locations{SLASH}{ANY_VALID_ROUTE}{SLASH}matches{OPTIONAL_SLASH}", StoolballRouteType.MatchesForMatchLocation },
                { $@"locations{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{OPTIONAL_SLASH}", StoolballRouteType.MatchLocationActions },
                { $@"locations{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{SLASH}location{OPTIONAL_SLASH}", StoolballRouteType.EditMatchLocation },
                { $@"locations{SLASH}{ANY_VALID_ROUTE}{SLASH}delete{OPTIONAL_SLASH}", StoolballRouteType.DeleteMatchLocation },
                { $@"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}add{OPTIONAL_SLASH}", StoolballRouteType.CreateSeason },
                { $@"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{OPTIONAL_SLASH}", StoolballRouteType.CompetitionActions },
                { $@"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{SLASH}competition{OPTIONAL_SLASH}", StoolballRouteType.EditCompetition },
                { $@"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}delete{OPTIONAL_SLASH}", StoolballRouteType.DeleteCompetition },
                { $@"matches{SLASH}{ANY_VALID_ROUTE}{SLASH}delete{OPTIONAL_SLASH}", StoolballRouteType.DeleteMatch },
                { $@"tournaments{SLASH}{ANY_VALID_ROUTE}{SLASH}delete{OPTIONAL_SLASH}", StoolballRouteType.DeleteTournament },

                // Match /tournaments/example123/teams/example-team, /tournaments/example123/teams/example-team/ or /tournaments/example123/teams/example-team/edit 
                // but not /tournaments/example123, /tournaments/example123/, /tournaments/example123/teams, /tournaments/example123/teams/,
                // /tournaments/example123/invalid or /tournaments/example123/teams/example-team/invalid in upper, lower or mixed case
                { $@"tournaments{SLASH}{ANY_VALID_ROUTE}{SLASH}teams{SLASH}{ANY_VALID_ROUTE}{OPTIONAL_SLASH}", StoolballRouteType.TransientTeam },
                { $@"tournaments{SLASH}{ANY_VALID_ROUTE}{SLASH}teams{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{OPTIONAL_SLASH}", StoolballRouteType.EditTransientTeam }
            };

            foreach (var routePattern in routeTypes.Keys)
            {
                if (Regex.IsMatch(path, $@"^{SLASH}{routePattern}$", RegexOptions.IgnoreCase))
                {
                    return routeTypes[routePattern];
                }
            }

            return null;
        }
    }
}