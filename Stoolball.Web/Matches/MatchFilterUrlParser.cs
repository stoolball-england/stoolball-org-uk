using System;
using System.Web;
using Stoolball.Matches;

namespace Stoolball.Web.Matches
{
    public class MatchFilterUrlParser : IMatchFilterUrlParser
    {
        public MatchFilter ParseUrl(Uri url)
        {
            if (url == null) { throw new ArgumentNullException(nameof(url)); }
            if (!url.IsAbsoluteUri) { throw new ArgumentException($"{nameof(url)} must be an absolute URL", nameof(url)); }

            var queryString = HttpUtility.ParseQueryString(url.Query);
            _ = DateTimeOffset.TryParse(queryString["from"], out var fromDate);
            _ = DateTimeOffset.TryParse(queryString["to"], out var untilDate);

            var filter = new MatchFilter
            {
                Query = queryString["q"]?.Trim(),
                FromDate = fromDate != DateTimeOffset.MinValue ? fromDate : (DateTimeOffset?)null,
                UntilDate = untilDate != DateTimeOffset.MinValue ? untilDate : (DateTimeOffset?)null
            };

            // Ensure the UntilDate filter is inclusive, by advancing from midnight at the start of the day to midnight at the end
            if (filter.UntilDate.HasValue) { filter.UntilDate = filter.UntilDate.Value.AddDays(1).AddSeconds(-1); }

            return filter;
        }
    }
}