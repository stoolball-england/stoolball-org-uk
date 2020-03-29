using Stoolball.Web.Clubs;
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
            { StoolballRouteType.Club, typeof(ClubController) }
        };

        [HttpGet]
        public new async Task<ActionResult> Index(ContentModel contentModel)
        {
            // Check the header from StoolballRouteContentFinder is present. If not, this controller was probably
            // invoked directly which is not intended, so return a 404.
            if (string.IsNullOrEmpty(Response.Headers[StoolballRouteContentFinder.ROUTE_TYPE_HEADER]))
            {
                return new HttpNotFoundResult();
            }

            // Check that the header value is valid. It should be since it's set as an enum value.
            if (!Enum.TryParse<StoolballRouteType>(Response.Headers[StoolballRouteContentFinder.ROUTE_TYPE_HEADER], out var routeType))
            {
                return new HttpNotFoundResult();
            }

            // Find the appropriate controller, and remove the header which we're now finished with.
            var controllerType = _supportedControllers[routeType];
            Response.Headers.Remove(StoolballRouteContentFinder.ROUTE_TYPE_HEADER);

            // Pass off the work of building a response to the appropriate controller.
            var controller = (RenderMvcControllerAsync)Current.Factory.GetInstance(controllerType);
            return await controller.Index(contentModel).ConfigureAwait(false);
        }
    }
}
