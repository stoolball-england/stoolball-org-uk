using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Stoolball.Web.Routing
{
    public class PreferredDomainRedirectMiddleware
    {
        private readonly RequestDelegate _next;

        public PreferredDomainRedirectMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (PreferredDomain.IsAlternateDomain(context.Request.Host.Host))
            {
                var path = UriHelper.GetEncodedPathAndQuery(context.Request);
                context.Response.Redirect("https://www.stoolball.org.uk" + path, true);
                return;
            }

            await _next(context);
        }
    }
}
