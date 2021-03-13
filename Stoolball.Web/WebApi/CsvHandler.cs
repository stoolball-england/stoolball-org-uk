using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Web;
using System.Web.Hosting;

namespace Stoolball.Web.WebApi
{
    public class CsvHandler : IHttpHandler
    {
        public bool IsReusable {
            // Return false in case your Managed Handler cannot be reused for another request.
            // Usually this would be false in case you have some state information preserved per request.
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            if (context is null)
            {
                throw new System.ArgumentNullException(nameof(context));
            }

            var requestedPath = Path.GetDirectoryName(context.Request.Url.AbsolutePath);
            if (!requestedPath.StartsWith(@"\DATA", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 404;
                return;
            }

            var requestedFile = Path.GetFileName(context.Request.Url.AbsolutePath);
            var path = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, @"App_Data\csv", requestedFile);
            if (!File.Exists(path))
            {
                context.Response.StatusCode = 404;
            }

            if (HasValidApiKey(context.Request.QueryString["key"]))
            {
                var extension = Path.GetExtension(requestedFile);
                var protectedPath = path.Substring(0, path.Length - extension.Length) + "-protected" + extension;
                if (File.Exists(protectedPath))
                {
                    path = protectedPath;
                }
            }

            context.Response.WriteFile(path);
        }

        private static bool HasValidApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey)) { return false; }

            var validApiKeys = ConfigurationManager.GetSection("StoolballApiKeys") as NameValueCollection;
            foreach (var key in validApiKeys.AllKeys)
            {
                if (validApiKeys[key] == apiKey) { return true; }
            }
            return false;
        }
    }
}
