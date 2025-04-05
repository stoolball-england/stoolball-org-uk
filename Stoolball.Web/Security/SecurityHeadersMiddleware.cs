using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Umbraco.Extensions;

namespace Stoolball.Web.Security
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string path = context.Request.Path;

            context.Response.Headers.Append("Referrer-Policy", "no-referrer-when-downgrade");
            context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000");
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN"); // SAMEORIGIN allows Umbraco 'Save and preview' to work
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Append("Report-To", "{\"group\":\"default\",\"max_age\":31536000,\"endpoints\":[{\"url\":\"https://stoolball.report-uri.com/a/d/g\"}],\"include_subdomains\":true}");
            context.Response.Headers.Append("NEL", "{\"report_to\":\"default\",\"max_age\":31536000,\"include_subdomains\":true}");

            if (path.StartsWith("/umbraco") == false && path.StartsWith("/install") == false)
            {
                context.Response.Headers.Append("Permissions-Policy", "accelerometer=(),ambient-light-sensor=(),autoplay=(),battery=(),camera=(),cross-origin-isolated=(),display-capture=(),document-domain=(),encrypted-media=(),execution-while-not-rendered=(),execution-while-out-of-viewport=(),fullscreen=(),geolocation=(),gyroscope=(),magnetometer=(),microphone=(),midi=(),navigation-override=(),payment=(),picture-in-picture=(),publickey-credentials-get=(),screen-wake-lock=(),sync-xhr=(),usb=(),web-share=(),xr-spatial-tracking=()");
                context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin; report-to=\"default\"");
            }

            // Add a CORP header to all client-side resources to prevent embedding on any third-party site that enables COEP
            if (new List<string> { ".css", ".js", ".svg", ".gif", ".png", ".jpg", ".webp", ".avif", ".ico" }.Contains(path.GetFileExtension().ToLowerInvariant()))
            {
                context.Response.Headers.Append("Cross-Origin-Resource-Policy", "same-origin");
            }

            await _next(context);
        }
    }
}
