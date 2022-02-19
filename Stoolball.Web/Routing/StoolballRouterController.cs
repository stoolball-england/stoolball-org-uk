using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Routing
{
    /// Controller for the 'Stoolball router' document type in Umbraco. This should only ever be invoked by 
    /// <see cref="StoolballRouteContentFinder" />, which passes the type of stoolball route it has recognised
    /// through to this controller as the action. This controller simply looks that route type up using the 
    /// <see cref="IStoolballRouteTypeMapper"/> and passes off the real work of building the response to the 
    /// appropriate controller.
    public class StoolballRouterController : RenderController, IStoolballRouterController
    {
        private readonly IStoolballRouteParser _routeParser;
        private readonly IStoolballRouteTypeMapper _routeTypeMapper;

        public StoolballRouterController(ILogger<StoolballRouterController> logger,
           ICompositeViewEngine compositeViewEngine,
           IUmbracoContextAccessor umbracoContextAccessor,
           IStoolballRouteParser routeParser,
           IStoolballRouteTypeMapper routeTypeMapper)
           : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _routeParser = routeParser ?? throw new ArgumentNullException(nameof(routeParser));
            _routeTypeMapper = routeTypeMapper ?? throw new ArgumentNullException(nameof(routeTypeMapper));
        }

        [HttpGet]
        [ServiceFilter(typeof(DelegatedContentSecurityPolicyAttribute))]
        public new async Task<IActionResult> Index()
        {
            if (string.IsNullOrEmpty(ControllerContext.RouteData.Values["slug"]?.ToString()))
            {
                return NotFound();
            }

            // Find the appropriate controller from the route value
            var routeType = _routeParser.ParseRouteType($"/{ ControllerContext.RouteData.Values["slug"]}");
            if (routeType == null)
            {
                return NotFound();
            }

            var controllerType = _routeTypeMapper.MapRouteTypeToController(routeType.Value);

            // Pass off the work of building a response to the appropriate controller.
            var controller = HttpContext.RequestServices.GetService(controllerType) as IRenderControllerAsync;
            if (controller == null) { throw new NotSupportedException($"{controllerType} is not registered with the dependency injection container."); }
            controller.ControllerContext = ControllerContext;
            controller.ModelState.Merge(ModelState);
            return await controller.Index();
        }
    }
}
