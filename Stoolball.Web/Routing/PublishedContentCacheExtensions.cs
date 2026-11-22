using System;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Routing
{
    public static class PublishedContentCacheExtensions
    {
        /// <summary>
        /// Gets published content by its route, for example <c>/stoolball-router</c>.
        /// </summary>
        /// <remarks>
        /// Replaces <c>IPublishedContentCache.GetByRoute</c>, which was removed in Umbraco 15.
        /// </remarks>
        /// <param name="contentCache">The published content cache to read the content from.</param>
        /// <param name="documentUrlService">The service that resolves a route to a document key.</param>
        /// <param name="route">The route of the content, relative to the root of the site.</param>
        /// <returns>The published content, or <c>null</c> if there is no published content at the route.</returns>
        public static IPublishedContent? GetByRoute(this IPublishedContentCache contentCache, IDocumentUrlService documentUrlService, string route)
        {
            ArgumentNullException.ThrowIfNull(contentCache);
            ArgumentNullException.ThrowIfNull(documentUrlService);

            var documentKey = documentUrlService.GetDocumentKeyByRoute(route, null, null, false);
            return documentKey.HasValue ? contentCache.GetById(documentKey.Value) : null;
        }
    }
}
