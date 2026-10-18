using System;
using System.Linq;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Stoolball.Web.Logging
{
    /// <summary>
    /// Drops telemetry that is not needed for tracking errors, to keep Application Insights under its daily cap.
    /// </summary>
    /// <remarks>
    /// <para>Successful SQL dependencies are dropped because they are most of the volume. Failed ones are kept.</para>
    /// <para>Requests for paths that only vulnerability scanners request are dropped. Each scanner request is recorded twice:
    /// once for the original 404, and again for /status/404.html, which serves the error page and has the original URL
    /// in its querystring after "404;".</para>
    /// </remarks>
    public class TelemetryCostFilterProcessor : ITelemetryProcessor
    {
        private const string ERROR_PAGE_PATH = "/status/404.html";

        private static readonly string[] _scannerPathPrefixes = ["/.env", "/.git/", "/wp-admin/", "/wp-content/", "/wp-includes/", "/@fs/", "/__vite", "/ipfs/"];
        private static readonly string[] _scannerPathSuffixes = [".php"];

        private readonly ITelemetryProcessor _next;

        public TelemetryCostFilterProcessor(ITelemetryProcessor next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public void Process(ITelemetry item)
        {
            if (item is DependencyTelemetry dependency && IsSuccessfulSqlDependency(dependency))
            {
                return;
            }

            if (item is RequestTelemetry request && IsScannerRequest(request))
            {
                return;
            }

            _next.Process(item);
        }

        private static bool IsSuccessfulSqlDependency(DependencyTelemetry dependency)
        {
            return dependency.Success == true && string.Equals(dependency.Type, "SQL", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsScannerRequest(RequestTelemetry request)
        {
            if (request.Url is null)
            {
                return false;
            }

            if (request.Url.AbsolutePath.Equals(ERROR_PAGE_PATH, StringComparison.OrdinalIgnoreCase))
            {
                var marker = request.Url.Query.IndexOf("404;", StringComparison.Ordinal);
                return marker > -1
                    && Uri.TryCreate(request.Url.Query[(marker + 4)..], UriKind.Absolute, out var originalUrl)
                    && IsScannerPath(originalUrl.AbsolutePath);
            }

            // Keep scanner paths that succeed, because that would be worth knowing about
            return request.Success != true && IsScannerPath(request.Url.AbsolutePath);
        }

        private static bool IsScannerPath(string path)
        {
            return _scannerPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                || _scannerPathSuffixes.Any(suffix => path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        }
    }
}
