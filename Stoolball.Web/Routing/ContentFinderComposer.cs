using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Routing;

namespace Stoolball.Web.Routing
{
    /// <summary>
    /// Registers the <see cref="StoolballRouteContentFinder"/> to run before the standard Umbraco content finder,
    /// so that if an Umbraco page is created with a URL that matches a route reserved for stoolball content, 
    /// the stoolball content wins.
    /// </summary>
    public class ContentFinderComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.ContentFinders().InsertBefore<ContentFinderByUrl, StoolballRouteContentFinder>();
        }
    }
}