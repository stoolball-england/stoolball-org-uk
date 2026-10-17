using Microsoft.AspNetCore.Builder;

namespace Stoolball.Web.Caching
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseStaticFileCaching(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<StaticFileCachingMiddleware>();
        }

        /// <summary>
        /// Cache RSS and ICS feeds, including failures. Requires <see cref="FeedOutputCachePolicy"/> to be registered as a base policy with AddOutputCache.
        /// </summary>
        public static IApplicationBuilder UseFeedOutputCaching(this IApplicationBuilder builder)
        {
            return builder.UseOutputCache().UseMiddleware<FeedFailureMiddleware>();
        }
    }
}
