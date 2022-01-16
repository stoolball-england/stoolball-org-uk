using Stoolball.Web.Security;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Current = Umbraco.Web.Composing.Current;

namespace Stoolball.Web.Routing
{
    /// Controller for the 'Stoolball router' document type in Umbraco. This should only ever be invoked by 
    /// <see cref="StoolballRouteContentFinder" />, which passes the type of stoolball route it has recognised
    /// through to this controller as the action. This controller simply looks that route type up using the 
    /// <see cref="IStoolballRouteTypeMapper"/> and passes off the real work of building the response to the 
    /// appropriate controller.
    public class StoolballRouterController : RenderMvcController, IStoolballRouterController
    {
        private readonly IStoolballRouteTypeMapper _routeTypeMapper;

        public StoolballRouterController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IStoolballRouteTypeMapper routeTypeMapper)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _routeTypeMapper = routeTypeMapper ?? throw new ArgumentNullException(nameof(routeTypeMapper));
        }

        [HttpGet]
        [DelegatedContentSecurityPolicy]
        public new async Task<ActionResult> Index(ContentModel contentModel)
        {
            // Find the appropriate controller from the route value
            var controllerType = _routeTypeMapper.MapRouteTypeToController(ControllerContext.RouteData.Values["action"].ToString());
            if (controllerType == null)
            {
                return new HttpNotFoundResult();
            }

            // Pass off the work of building a response to the appropriate controller.
            var controller = (RenderMvcControllerAsync)Current.Factory.GetInstance(controllerType);
            controller.ControllerContext = ControllerContext;
            controller.ModelState.Merge(ModelState);
            return await controller.Index(contentModel).ConfigureAwait(false);
        }
    }
}
