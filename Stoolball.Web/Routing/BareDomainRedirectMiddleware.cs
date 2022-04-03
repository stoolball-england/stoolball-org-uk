using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Stoolball.Web.Routing
{
    public class BareDomainRedirectMiddleware
    {
        private readonly RequestDelegate _next;

        public BareDomainRedirectMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var domain = context.Request.Host.Host.ToUpperInvariant();
            var path = UriHelper.GetEncodedPathAndQuery(context.Request);

            if (domain == "STOOLBALL.ORG.UK")
            {
                context.Response.Redirect("https://www.stoolball.org.uk" + path, true);
                return;
            }

            await _next(context);
        }
    }
}
