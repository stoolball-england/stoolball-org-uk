using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;

namespace Stoolball.Web.Security
{
    public class ContentSecurityPolicyAttribute : ActionFilterAttribute, IActionFilter
    {
        private Dictionary<string, IList<string>> _directives;
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
        private const string SOURCE_SEPARATOR = " ";
        private const string DIRECTIVE_SEPARATOR = ";";

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

            SetupDirectives();
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

            filterContext.HttpContext.Response.Headers.Add("Content-Security-Policy", CreatePolicy(DEFAULT_SRC, STYLE_SRC, SCRIPT_SRC, IMG_SRC, FONT_SRC, FRAME_SRC, CONNECT_SRC, MANIFEST_SRC, TRUSTED_TYPES, REPORT_URI));
            filterContext.HttpContext.Response.Headers.Add("X-Content-Security-Policy", CreatePolicy(DEFAULT_SRC, STYLE_SRC, SCRIPT_SRC, IMG_SRC, FONT_SRC, FRAME_SRC, MANIFEST_SRC, CONNECT_SRC, REPORT_URI));
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
                    policy.Append(string.Join(SOURCE_SEPARATOR, _directives[directive]));
                }
            }
            return policy.ToString();
        }

        private void SetupForms()
        {
            // Used by jQuery validation for valid/invalid icons in the field
            AddSource(IMG_SRC, "data:");
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
            AddSource(REPORT_URI, "https://stoolball.report-uri.com/r/d/csp/enforce");
        }

        private void SetupDirectives()
        {
            _directives = new Dictionary<string, IList<string>>();
            AddDirective(DEFAULT_SRC);
            AddDirective(STYLE_SRC);
            AddDirective(SCRIPT_SRC);
            AddDirective(IMG_SRC);
            AddDirective(FONT_SRC);
            AddDirective(CONNECT_SRC);
            AddDirective(TRUSTED_TYPES);
            AddDirective(MANIFEST_SRC);
            AddDirective(FRAME_SRC);
            AddDirective(REPORT_URI);
        }

        private void AddDirective(string directive)
        {
            if (!_directives.ContainsKey(directive))
            {
                _directives.Add(directive, new List<string>());
            }
        }
    }
}