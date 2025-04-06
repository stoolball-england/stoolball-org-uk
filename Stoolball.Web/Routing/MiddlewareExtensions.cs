using Microsoft.AspNetCore.Builder;

namespace Stoolball.Web.Routing
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseBareDomainRedirect(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PreferredDomainRedirectMiddleware>();
        }
    }
}
