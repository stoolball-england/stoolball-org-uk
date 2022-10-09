using System;

namespace Stoolball
{
    public class UrlFormatter : IUrlFormatter
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "Needs to accept values without a protocol like www.example.org")]
        public Uri? PrefixHttpsProtocol(string? url)
        {
            if (url == null) return null;

            url = url.Trim();
            if (!string.IsNullOrEmpty(url) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return new Uri("https://" + url);
            }
            return null;
        }
    }
}
