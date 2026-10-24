using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Rewrite;

namespace Stoolball.Web.Routing
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseBareDomainRedirect(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PreferredDomainRedirectMiddleware>();
        }

        /// <summary>
        /// Rewrites the .rss and .ics extensions to /rss and /ics, which ASP.NET and Umbraco are set up to handle.
        /// </summary>
        /// <remarks>
        /// IIS does this using a rule in web.config, but Kestrel does not read web.config. Under IIS the path has already been rewritten, so this rule does not match.
        /// </remarks>
        public static IApplicationBuilder UseFeedExtensionRewrite(this IApplicationBuilder builder)
        {
            return builder.UseRewriter(new RewriteOptions().AddRewrite(@"(?i)^(.*)\.(rss|ics)$", "$1/$2", skipRemainingRules: true));
        }
    }
}
