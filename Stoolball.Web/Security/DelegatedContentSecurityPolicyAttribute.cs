using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Stoolball.Web.Routing;

namespace Stoolball.Web.Security
{
    /// <summary>
    /// Specifically designed to apply to <see cref="StoolballRouterController"/>, which delegates its work to a controller it looks
    /// up using an <see cref="IStoolballRouteTypeMapper"/>. Doing that doesn't run the action filters on the looked-up controller,
    /// so this derived type looks up an attribute matching its base type and sets the properties from there, effectively impersonating 
    /// that attribute. This means that <see cref="ContentSecurityPolicyAttribute"/> can be applied to all controllers without worrying 
    /// about how they're routed.
    /// </summary>
    public class DelegatedContentSecurityPolicyAttribute : ContentSecurityPolicyAttribute
    {
        private readonly IStoolballRouteParser _routeParser;
        private readonly IStoolballRouteTypeMapper _routeTypeMapper;

        public DelegatedContentSecurityPolicyAttribute(IStoolballRouteParser routeParser, IStoolballRouteTypeMapper routeTypeMapper)
        {
            _routeParser = routeParser ?? throw new ArgumentNullException(nameof(routeParser));
            _routeTypeMapper = routeTypeMapper ?? throw new ArgumentNullException(nameof(routeTypeMapper));
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (filterContext is null)
            {
                throw new ArgumentNullException(nameof(filterContext));
            }

            var routeType = _routeParser.ParseRouteType($"/{filterContext.RouteData.Values["umbracoSlug"]}");
            if (routeType != null)
            {
                var controllerType = _routeTypeMapper.MapRouteTypeToController(routeType.Value);

                var cspAttribute = controllerType.GetMethod("Index")!.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(ContentSecurityPolicyAttribute));
                if (cspAttribute != null)
                {
                    GoogleMaps = cspAttribute.NamedArguments.Where(x => x.MemberName == "GoogleMaps").Select(x => (bool)x.TypedValue.Value!).FirstOrDefault();
                    GoogleGeocode = cspAttribute.NamedArguments.Where(x => x.MemberName == "GoogleGeocode").Select(x => (bool)x.TypedValue.Value!).FirstOrDefault();
                    Forms = cspAttribute.NamedArguments.Where(x => x.MemberName == "Forms").Select(x => (bool)x.TypedValue.Value!).FirstOrDefault();
                    TinyMCE = cspAttribute.NamedArguments.Where(x => x.MemberName == "TinyMCE").Select(x => (bool)x.TypedValue.Value!).FirstOrDefault();
                }
            }

            base.OnResultExecuted(filterContext);
        }
    }
}