using Stoolball.Web.Clubs;
using Stoolball.Web.Competitions;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.Teams;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Current = Umbraco.Web.Composing.Current;

namespace Stoolball.Web.Routing
{
    /// Controller for the 'Stoolball router' document type in Umbraco. This should only ever be invoked by 
    /// <see cref="StoolballRouteContentFinder" />, which passes the type of stoolball route it has recognised
    /// through to this controller in a response header. This controller simply looks that route type up in 
    /// a dictionary and passes off the real work of building the response to the appropriate controller.
    public class StoolballRouterController : RenderMvcController
    {
        private readonly Dictionary<StoolballRouteType, Type> _supportedControllers = new Dictionary<StoolballRouteType, Type> {
            { StoolballRouteType.Club, typeof(ClubController) },
            { StoolballRouteType.Team, typeof(TeamController) },
            { StoolballRouteType.MatchLocation, typeof(MatchLocationController) },
            { StoolballRouteType.Competition, typeof(CompetitionController) },
            { StoolballRouteType.Season, typeof(SeasonController) }
        };

        [HttpGet]
        public new async Task<ActionResult> Index(ContentModel contentModel)
        {
            // Check that the header value is valid. It should be since it's set as an enum value.
            if (!Enum.TryParse<StoolballRouteType>(ControllerContext.RouteData.Values["action"].ToString(), true, out var routeType))
            {
                return new HttpNotFoundResult();
            }

            // Find the appropriate controller, and remove the header which we're now finished with.
            var controllerType = _supportedControllers[routeType];

            // Pass off the work of building a response to the appropriate controller.
            var controller = (RenderMvcControllerAsync)Current.Factory.GetInstance(controllerType);
            controller.ControllerContext = ControllerContext;
            return await controller.Index(contentModel).ConfigureAwait(false);
        }
    }
}
