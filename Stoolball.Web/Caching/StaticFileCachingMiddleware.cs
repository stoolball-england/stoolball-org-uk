using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Umbraco.Extensions;

namespace Stoolball.Web.Caching
{
    /// <summary>
    /// Add Cache-Control header to cache static files for 1 year
    /// </summary>
    public class StaticFileCachingMiddleware
    {
        private readonly RequestDelegate _next;

        public StaticFileCachingMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string path = context.Request.Path;

            if (path.StartsWith("/umbraco/") == false && path.StartsWith("/install") == false)
            {
                if (new List<string> { ".css", ".js", ".svg", ".gif", ".png", ".jpg", ".ico", ".woff", ".woff2" }.Contains(path.GetFileExtension().ToLowerInvariant()))
                {
                    context.Response.Headers.Append("Cache-Control", "public, max-age=31536000");
                }
            }

            await _next(context);
        }
    }
}
