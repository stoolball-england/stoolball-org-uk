using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Stoolball.Filtering
{
    public abstract class BaseFilterQueryStringParser
    {
        protected static (DateTimeOffset? fromDate, DateTimeOffset? untilDate) ParseDateFilter(DateTimeOffset? fromDate, DateTimeOffset? untilDate, Dictionary<string, StringValues> queryString)
        {
            if (queryString is null)
            {
                throw new ArgumentNullException(nameof(queryString));
            }

            if (queryString.ContainsKey("from"))
            {
                fromDate = DateTimeOffset.TryParse(queryString["from"], out var parsedFromDate) ? parsedFromDate : (DateTimeOffset?)null;
            }

            if (queryString.ContainsKey("to"))
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
