using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Stoolball.Web.Security
{
    public class ContentSecurityPolicyAttribute : ActionFilterAttribute, IActionFilter
    {
        private Dictionary<string, IList<string>> _directives = new()
        {
            { DEFAULT_SRC, new List<string>() },
            { STYLE_SRC, new List<string>() },
            { SCRIPT_SRC, new List<string>() },
            { IMG_SRC, new List<string>() },
            { FONT_SRC, new List<string>() },
            { CONNECT_SRC, new List<string>() },
            { TRUSTED_TYPES, new List<string>() },
            { MANIFEST_SRC, new List<string>() },
            { FRAME_SRC, new List<string>() },
            { WORKER_SRC, new List<string>() },
            { REPORT_URI, new List<string>() },
            { REPORT_TO, new List<string>() }
        };
#pragma warning disable IDE1006 // Naming Styles, intended for private fields rather than constants.
        private const string DEFAULT_SRC = "default-src";
        private const string STYLE_SRC = "style-src";
        private const string SCRIPT_SRC = "script-src";
        private const string IMG_SRC = "img-src";
        private const string FONT_SRC = "font-src";
        private const string FRAME_SRC = "frame-src";
        private const string CONNECT_SRC = "connect-src";
        private const string TRUSTED_TYPES = "require-trusted-types-for";
        private const string MANIFEST_SRC = "manifest-src";
        private const string REPORT_URI = "report-uri";
        private const string REPORT_TO = "report-to";
        private const string WORKER_SRC = "worker-src";
        private const char SOURCE_SEPARATOR = ' ';
        private const char DIRECTIVE_SEPARATOR = ';';
#pragma warning restore IDE1006 // Naming Styles

        public bool GoogleMaps { get; set; }

        public bool GoogleGeocode { get; set; }

        public bool Forms { get; set; }

        public bool TinyMCE { get; set; }

        public bool GettyImages { get; set; }

        public bool YouTube { get; set; }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (filterContext is null)
            {
                throw new System.ArgumentNullException(nameof(filterContext));
            }

            SetupDefaultPolicy();
            SetupCloudFlareAnalytics();

            if (GoogleMaps)
            {
                SetupGoogleMapsApi();
            }

            if (GoogleGeocode)
            {
                SetupGoogleGeocodeApi();
            }

            if (Forms)
            {
                SetupForms();
            }

            if (GettyImages)
            {
                SetupGettyImages();
            }

            if (YouTube)
            {
                SetupYouTube();
            }

            if (TinyMCE)
            {
                SetupTinyMCE();
            }
            else
            {
                SetupTrustedTypes();
            }

            if (!filterContext.HttpContext.Response.HasStarted && !filterContext.HttpContext.Response.Headers.ContainsKey("Content-Security-Policy"))
            {
                filterContext.HttpContext.Response.Headers.Add("Content-Security-Policy", CreatePolicy(DEFAULT_SRC, STYLE_SRC, SCRIPT_SRC, IMG_SRC, FONT_SRC, FRAME_SRC, CONNECT_SRC, MANIFEST_SRC, TRUSTED_TYPES, WORKER_SRC, REPORT_URI, REPORT_TO));
            }
            if (!filterContext.HttpContext.Response.HasStarted && !filterContext.HttpContext.Response.Headers.ContainsKey("X-Content-Security-Policy"))
            {
                filterContext.HttpContext.Response.Headers.Add("X-Content-Security-Policy", CreatePolicy(DEFAULT_SRC, STYLE_SRC, SCRIPT_SRC, IMG_SRC, FONT_SRC, FRAME_SRC, MANIFEST_SRC, CONNECT_SRC, REPORT_URI));
            }
        }

        private void SetupCloudFlareAnalytics()
        {
            AddSource(SCRIPT_SRC, "https://static.cloudflareinsights.com");
            AddSource(CONNECT_SRC, "https://cloudflareinsights.com");
        }

        private void SetupGettyImages()
        {
            AddSource(FRAME_SRC, "https://embed.gettyimages.com");
        }

        private void SetupYouTube()
        {
            AddSource(FRAME_SRC, "https://www.youtube-nocookie.com");
            AddSource(IMG_SRC, "https://i.ytimg.com");
        }

        private void SetupTinyMCE()
        {
            AddSource(STYLE_SRC, "'unsafe-inline'");
        }

        private void AddSource(string directive, string source)
        {
            if (!_directives[directive].Contains(source))
            {
                _directives[directive].Add(source);
            }
        }

        private string CreatePolicy(params string[] directivesToInclude)
        {
            var policy = new StringBuilder();
            foreach (var directive in directivesToInclude)
            {
                if (policy.Length > 0) { policy.Append(DIRECTIVE_SEPARATOR); }
                if (_directives[directive].Count > 0)
                {
                    policy.Append(directive).Append(SOURCE_SEPARATOR);
                    policy.Append(string.Join(SOURCE_SEPARATOR.ToString(), _directives[directive]));
                }
            }
            return policy.ToString();
        }

        private void SetupForms()
        {
            // Used by jQuery validation for valid/invalid icons in the field
            AddSource(IMG_SRC, "data:");

            // For style="display: none" used in conditions
            AddSource(STYLE_SRC, "'unsafe-inline'");
        }

        private void SetupTrustedTypes()
        {
            AddSource(TRUSTED_TYPES, "'script'");
        }

        private void SetupGoogleGeocodeApi()
        {
            AddSource(CONNECT_SRC, "https://maps.googleapis.com");
        }

        private void SetupGoogleMapsApi()
        {
            AddSource(SCRIPT_SRC, "https://www.google.com");
            AddSource(SCRIPT_SRC, "https://maps.google.com");
            AddSource(SCRIPT_SRC, "https://maps.google.co.uk");
            AddSource(SCRIPT_SRC, "https://maps.googleapis.com");
            AddSource(STYLE_SRC, "https://fonts.googleapis.com");
            AddSource(STYLE_SRC, "'unsafe-inline'");
            AddSource(IMG_SRC, "https://maps.gstatic.com");
            AddSource(IMG_SRC, "https://maps.google.co.uk");
            AddSource(IMG_SRC, "https://maps.googleapis.com");
            AddSource(IMG_SRC, "https://khms0.googleapis.com");
            AddSource(IMG_SRC, "https://khms1.googleapis.com");
            AddSource(IMG_SRC, "data:");
            AddSource(FONT_SRC, "https://fonts.gstatic.com");
        }

        private void SetupDefaultPolicy()
        {
            AddSource(DEFAULT_SRC, "'none'");
            AddSource(STYLE_SRC, "'self'");
            AddSource(SCRIPT_SRC, "'self'");
            AddSource(IMG_SRC, "'self'");
            AddSource(IMG_SRC, "https://s.gravatar.com/avatar/");
            AddSource(IMG_SRC, "data:"); // for Bootstrap custom checkboxes in the cookie banner
            AddSource(CONNECT_SRC, "'self'");
            AddSource(MANIFEST_SRC, "'self'");
            AddSource(FONT_SRC, "'self'");
            AddSource(WORKER_SRC, "'self'");
            AddSource(REPORT_URI, "https://stoolball.report-uri.com/r/d/csp/enforce");
            AddSource(REPORT_TO, "default");
        }
    }
}