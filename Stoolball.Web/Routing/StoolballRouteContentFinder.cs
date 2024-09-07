using System;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;

namespace Stoolball.Web.Routing
{
    /// <summary>
    /// Looks for routes that correspond to stoolball entities, and directs them to an 
    /// instance of the 'Stoolball router' document type where it's handled by <see cref="StoolballRouterController"/>.
    /// </summary>
    public class StoolballRouteContentFinder : IContentFinder
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IStoolballRouteParser _routeParser;

        public StoolballRouteContentFinder(IUmbracoContextAccessor umbracoContextAccessor, IStoolballRouteParser routeParser)
        {
            _umbracoContextAccessor = umbracoContextAccessor ?? throw new ArgumentNullException(nameof(umbracoContextAccessor));
            _routeParser = routeParser ?? throw new ArgumentNullException(nameof(routeParser));
        }

        Task<bool> IContentFinder.TryFindContent(IPublishedRequestBuilder request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
            {
                return Task.FromResult(false);
            }

            var matchedRouteType = _routeParser.ParseRouteType(request.Uri.GetAbsolutePathDecoded());
            if (matchedRouteType.HasValue)
            {
                // Direct the response to the 'Stoolball router' document type to be handled by StoolballRouterController
                var router = umbracoContext.Content!.GetSingleByXPath("//stoolballRouter");

                if (router != null)
                {
                    request.SetPublishedContent(router);
                    request.TrySetTemplate(matchedRouteType.Value.ToString());
                    return Task.FromResult(request.HasTemplate() && router.IsAllowedTemplate(request.Template!.Alias));
                }
            }

            return Task.FromResult(false);
        }
    }
}