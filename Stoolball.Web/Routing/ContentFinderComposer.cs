using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Web;
using Umbraco.Web.Routing;

namespace Stoolball.Web.Routing
{
    /// <summary>
    /// Registers the <see cref="StoolballRouteContentFinder"/> to run before the standard Umbraco content finder,
    /// so that if an Umbraco page is created with a URL that matches a route reserved for stoolball content, 
    /// the stoolball content wins.
    /// </summary>
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class ContentFinderComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.ContentFinders().InsertBefore<ContentFinderByUrl, StoolballRouteContentFinder>();
        }
    }
}