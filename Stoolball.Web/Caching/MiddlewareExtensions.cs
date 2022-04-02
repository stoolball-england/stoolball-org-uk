using Microsoft.AspNetCore.Builder;

namespace Stoolball.Web.Caching
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseStaticFileCaching(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<StaticFileCachingMiddleware>();
        }
    }
}
