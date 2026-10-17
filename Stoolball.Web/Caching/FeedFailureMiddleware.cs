using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;

namespace Stoolball.Web.Caching
{
    /// <summary>
    /// Turns an unhandled exception from an RSS or ICS feed into a 503 response, which <see cref="FeedOutputCachePolicy"/>
    /// caches so that pollers stop retrying an expensive query that is failing.
    /// </summary>
    public class FeedFailureMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<FeedFailureMiddleware> _logger;

        public FeedFailureMiddleware(RequestDelegate next, ILogger<FeedFailureMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!FeedPaths.IsFeed(context.Request.Path))
            {
                await _next(context);
                return;
            }

            try
            {
                await _next(context);
            }
            catch (Exception ex) when (!context.Response.HasStarted && !context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogError(ex, "Feed {Path} failed. Returning 503, which will be cached for {Expiration}.", context.Request.Path.Value, FeedOutputCachePolicy.FailureExpiration);

                // The output cache fixes the expiration when the response starts, which is before FeedOutputCachePolicy.ServeResponseAsync runs
                var outputCache = context.Features.Get<IOutputCacheFeature>();
                if (outputCache is not null)
                {
                    outputCache.Context.ResponseExpirationTimeSpan = FeedOutputCachePolicy.FailureExpiration;
                }

                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.Headers.RetryAfter = ((int)FeedOutputCachePolicy.FailureExpiration.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("This feed is temporarily unavailable. Please try again later.");
            }
        }
    }
}
