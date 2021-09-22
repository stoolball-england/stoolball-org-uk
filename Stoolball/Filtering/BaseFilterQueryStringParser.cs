using System;
using System.Collections.Specialized;

namespace Stoolball.Filtering
{
    public abstract class BaseFilterQueryStringParser
    {
        protected static (DateTimeOffset? fromDate, DateTimeOffset? untilDate) ParseDateFilter(DateTimeOffset? fromDate, DateTimeOffset? untilDate, NameValueCollection queryString)
        {
            if (queryString is null)
            {
                throw new ArgumentNullException(nameof(queryString));
            }

            if (queryString["from"] != null)
            {
                fromDate = DateTimeOffset.TryParse(queryString["from"], out var parsedFromDate) ? parsedFromDate : (DateTimeOffset?)null;
            }

            if (queryString["to"] != null)
            {
                if (DateTimeOffset.TryParse(queryString["to"], out var parsedUntilDate))
                {
                    // Ensure the UntilDate filter is inclusive, by advancing from midnight at the start of the day to midnight at the end
                    untilDate = parsedUntilDate.Date.AddDays(1).AddSeconds(-1);
                }
                else
                {
                    untilDate = null;
                }
            }

            return (fromDate, untilDate);
        }
    }
}
