using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Stoolball.Web.Configuration;

namespace Stoolball.Web.Export
{
    public class CsvMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IApiKeyProvider _apiKeyProvider;

        public CsvMiddleware(RequestDelegate next,
            IWebHostEnvironment webHostEnvironment,
            IApiKeyProvider apiKeyProvider)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _apiKeyProvider = apiKeyProvider ?? throw new ArgumentNullException(nameof(apiKeyProvider));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestedPath = Path.GetDirectoryName(context.Request.Path) ?? string.Empty;
            if (requestedPath.StartsWith(@"\DATA", StringComparison.OrdinalIgnoreCase))
            {
                var requestedFile = Path.GetFileName(context.Request.Path);
                var path = Path.Combine(_webHostEnvironment.ContentRootPath, @"App_Data\csv", requestedFile);
                var extension = Path.GetExtension(requestedFile);
                if (System.IO.File.Exists(path))
                {
                    await RespondWithFileAsync(context.Response, path);
                    return;
                }

                var protectedPath = path.Substring(0, path.Length - extension.Length) + "-protected" + extension;
                if (System.IO.File.Exists(protectedPath) &&
                   context.Request.Query.ContainsKey("key") &&
                   HasValidApiKey(context.Request.Query["key"]))
                {
                    await RespondWithFileAsync(context.Response, protectedPath);
                    return;
                }
            }

            await _next(context);
        }

        private static async Task RespondWithFileAsync(HttpResponse response, string path)
        {
            response.ContentType = "text/csv";
            response.Headers.Add("Cache-Control", "max-age=86400, public"); // 1 day
            response.Headers.Add("Content-Disposition", "attachment; filename=\"stoolball-england.csv\"");
            await response.SendFileAsync(path);
        }

        private bool HasValidApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey)) { return false; }
            return _apiKeyProvider.GetApiKey("CsvExport") == apiKey;
        }
    }
}
