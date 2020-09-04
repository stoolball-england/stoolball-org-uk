using System.Collections.Generic;
using System.Text;
using System.Threading;
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
        private const string CONNECT_SRC = "connect-src";
        private const string TRUSTED_TYPES = "require-trusted-types-for";
        private const string MANIFEST_SRC = "manifest-src";
        private const string SOURCE_SEPARATOR = " ";
        private const string DIRECTIVE_SEPARATOR = ";";

        public bool GoogleMaps { get; set; }

        public bool GoogleGeocode { get; set; }

        public bool Forms { get; set; }

        public bool TinyMCE { get; set; }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (filterContext is null)
            {
                throw new System.ArgumentNullException(nameof(filterContext));
            }

            SetupDirectives();
            SetupDefaultPolicy();

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

            if (TinyMCE)
            {
                SetupTinyMCE();
            }
            else
            {
                SetupTrustedTypes();
            }

            filterContext.HttpContext.Response.Headers.Add("Content-Security-Policy", CreatePolicy(DEFAULT_SRC, STYLE_SRC, SCRIPT_SRC, IMG_SRC, FONT_SRC, CONNECT_SRC, MANIFEST_SRC, TRUSTED_TYPES));
            filterContext.HttpContext.Response.Headers.Add("X-Content-Security-Policy", CreatePolicy(DEFAULT_SRC, STYLE_SRC, SCRIPT_SRC, IMG_SRC, FONT_SRC, MANIFEST_SRC, CONNECT_SRC));
        }

        private void SetupTinyMCE()
        {
            AddSource(STYLE_SRC, "'unsafe-inline'");
            AddSource(FONT_SRC, "'self'"); // in Firefox, but not Chrome
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
                policy.Append(directive).Append(SOURCE_SEPARATOR);
                policy.Append(string.Join(SOURCE_SEPARATOR, _directives[directive]));
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
            AddSource(CONNECT_SRC, "'self'");
            AddSource(MANIFEST_SRC, "'self'");
        }

        private void SetupDirectives()
        {
            _directives = new Dictionary<string, IList<string>>();
            _directives.Add(DEFAULT_SRC, new List<string>());
            _directives.Add(STYLE_SRC, new List<string>());
            _directives.Add(SCRIPT_SRC, new List<string>());
            _directives.Add(IMG_SRC, new List<string>());
            _directives.Add(FONT_SRC, new List<string>());
            _directives.Add(CONNECT_SRC, new List<string>());
            _directives.Add(TRUSTED_TYPES, new List<string>());
            _directives.Add(MANIFEST_SRC, new List<string>());
        }
    }
}