using System;
using System.Collections.Specialized;
using Stoolball.Filtering;

namespace Stoolball.Statistics
{
    public class StatisticsFilterQueryStringParser : BaseFilterQueryStringParser, IStatisticsFilterQueryStringParser
    {
        public StatisticsFilter ParseQueryString(StatisticsFilter filter, NameValueCollection queryString)
        {
            if (filter == null) { throw new ArgumentNullException(nameof(filter)); }
            if (queryString == null) { throw new ArgumentNullException(nameof(queryString)); }

            var updatedFilter = filter.Clone();

            var (fromDate, untilDate) = ParseDateFilter(updatedFilter.FromDate, updatedFilter.UntilDate, queryString);
            updatedFilter.FromDate = fromDate;
            updatedFilter.UntilDate = untilDate;

            return updatedFilter;
        }
    }
}