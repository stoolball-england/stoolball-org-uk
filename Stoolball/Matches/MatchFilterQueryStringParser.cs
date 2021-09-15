using System;
using System.Collections.Specialized;

namespace Stoolball.Matches
{
    public class MatchFilterQueryStringParser : IMatchFilterQueryStringParser
    {
        public MatchFilter ParseQueryString(MatchFilter filter, NameValueCollection queryString)
        {
            if (filter == null) { throw new ArgumentNullException(nameof(filter)); }
            if (queryString == null) { throw new ArgumentNullException(nameof(queryString)); }

            var updatedFilter = filter.Clone();
            updatedFilter.Query = queryString["q"]?.Trim();

            if (queryString["from"] != null)
            {
                updatedFilter.FromDate = DateTimeOffset.TryParse(queryString["from"], out var fromDate) ? fromDate : (DateTimeOffset?)null;
            }

            if (queryString["to"] != null)
            {
                if (DateTimeOffset.TryParse(queryString["to"], out var untilDate))
                {
                    // Ensure the UntilDate filter is inclusive, by advancing from midnight at the start of the day to midnight at the end
                    updatedFilter.UntilDate = untilDate.Date.AddDays(1).AddSeconds(-1);
                }
                else
                {
                    updatedFilter.UntilDate = null;
                }
            }

            return updatedFilter;
        }
    }
}