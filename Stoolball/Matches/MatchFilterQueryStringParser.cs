﻿using System;
using System.Collections.Specialized;
using Stoolball.Filtering;

namespace Stoolball.Matches
{
    public class MatchFilterQueryStringParser : BaseFilterQueryStringParser, IMatchFilterQueryStringParser
    {
        public MatchFilter ParseQueryString(MatchFilter filter, NameValueCollection queryString)
        {
            if (filter == null) { throw new ArgumentNullException(nameof(filter)); }
            if (queryString == null) { throw new ArgumentNullException(nameof(queryString)); }

            var updatedFilter = filter.Clone();
            updatedFilter.Query = queryString["q"]?.Trim();

            var (fromDate, untilDate) = ParseDateFilter(updatedFilter.FromDate, updatedFilter.UntilDate, queryString);
            updatedFilter.FromDate = fromDate;
            updatedFilter.UntilDate = untilDate;

            return updatedFilter;
        }
    }
}