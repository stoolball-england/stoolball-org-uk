using Microsoft.AspNetCore.Builder;

namespace Stoolball.Web.Security
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
