using System;
using Microsoft.AspNetCore.Http;

namespace Stoolball.Web.Caching
{
    /// <summary>
    /// Recognises requests for RSS and ICS feeds, which are polled automatically by feed readers and calendar apps
    /// </summary>
    public static class FeedPaths
    {
        public static bool IsFeed(PathString path)
        {
            if (!path.HasValue) { return false; }

            var value = path.Value!.TrimEnd('/');
            return value.EndsWith("/rss", StringComparison.OrdinalIgnoreCase) ||
                   value.EndsWith("/ics", StringComparison.OrdinalIgnoreCase) ||
                   value.EndsWith(".ics", StringComparison.OrdinalIgnoreCase);
        }
    }
}
