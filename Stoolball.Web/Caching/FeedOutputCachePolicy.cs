using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;

namespace Stoolball.Web.Caching
{
    /// <summary>
    /// Caches RSS and ICS feeds in memory. Successful responses are cached briefly to absorb repeated polling.
    /// Failures (a 503 from <see cref="FeedFailureMiddleware"/>) are cached for longer, so that a feed whose query
    /// times out does not run that expensive query again every time it is polled.
    /// </summary>
    public class FeedOutputCachePolicy : IOutputCachePolicy
    {
        public static TimeSpan SuccessExpiration { get; } = TimeSpan.FromMinutes(15);
        public static TimeSpan FailureExpiration { get; } = TimeSpan.FromHours(1);

        public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation)
        {
            var request = context.HttpContext.Request;
            var cacheable = HttpMethods.IsGet(request.Method) && FeedPaths.IsFeed(request.Path);

            context.EnableOutputCaching = cacheable;
            context.AllowCacheLookup = cacheable;
            context.AllowCacheStorage = cacheable;
            context.AllowLocking = true;
            context.CacheVaryByRules.QueryKeys = "*";
            context.ResponseExpirationTimeSpan = SuccessExpiration;

            return ValueTask.CompletedTask;
        }

        public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation) => ValueTask.CompletedTask;

        public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
        {
            // Expiration can't be set here because the output cache fixes it when the response starts, which is usually before this runs.
            // FeedFailureMiddleware sets the expiration for failures.
            var response = context.HttpContext.Response;
            var cacheableStatus = response.StatusCode == StatusCodes.Status200OK || response.StatusCode == StatusCodes.Status503ServiceUnavailable;

            if (!cacheableStatus || !StringValues.IsNullOrEmpty(response.Headers.SetCookie))
            {
                context.AllowCacheStorage = false;
            }

            return ValueTask.CompletedTask;
        }
    }
}
